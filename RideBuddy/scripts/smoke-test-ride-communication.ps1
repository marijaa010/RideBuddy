# RideBuddy Ride-Service communication smoke test.
# Runs end-to-end checks for User <-> Ride and Booking -> Ride/User gRPC flows.
param(
    [string]$ComposeProjectRoot = "",
    [switch]$SkipBuild,
    [switch]$SkipDirectGrpcCheck,
    [switch]$IncludeCancellationCheck,
    [switch]$IncludeRideEndpointChecks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param([string]$StartPath)

    $current = Resolve-Path $StartPath
    while ($true) {
        if (Test-Path (Join-Path $current "docker-compose.yml")) {
            return $current.Path
        }

        $parent = Split-Path $current -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            throw "Could not locate repository root containing docker-compose.yml."
        }

        $current = Resolve-Path $parent
    }
}

function Wait-ForHealth {
    param(
        [string]$Name,
        [string]$Url,
        [int]$TimeoutSeconds = 180
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                Write-Host "[OK] $Name healthy at $Url"
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "$Name health endpoint did not become ready within $TimeoutSeconds seconds: $Url"
}

function Test-CommandAvailable {
    param([string]$CommandName)
    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Invoke-JsonPost {
    param(
        [string]$Uri,
        [hashtable]$Body,
        [hashtable]$Headers = @{}
    )

    return Invoke-RestMethod -Method Post -Uri $Uri -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 10) -Headers $Headers
}

if ([string]::IsNullOrWhiteSpace($ComposeProjectRoot)) {
    $ComposeProjectRoot = Resolve-RepoRoot -StartPath $PSScriptRoot
}

if (-not (Test-Path (Join-Path $ComposeProjectRoot "docker-compose.yml"))) {
    throw "docker-compose.yml not found under ComposeProjectRoot: $ComposeProjectRoot"
}

Write-Host "[INFO] Repo root: $ComposeProjectRoot"
Push-Location $ComposeProjectRoot
try {
    if ($SkipBuild) {
        Write-Host "[STEP] Starting stack without build"
        docker compose up -d
    }
    else {
        Write-Host "[STEP] Starting stack with build"
        docker compose up --build -d
    }

    Write-Host "[STEP] Waiting for services"
    Wait-ForHealth -Name "User Service" -Url "http://localhost:5001/health"
    Wait-ForHealth -Name "Ride Service" -Url "http://localhost:5002/health"
    Wait-ForHealth -Name "Booking Service" -Url "http://localhost:5003/health"

    $suffix = [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $driverEmail = "driver.$suffix@test.com"
    $passengerEmail = "passenger.$suffix@test.com"
    $password = "Pass123!"

    Write-Host "[STEP] Registering users"
    [void](Invoke-JsonPost -Uri "http://localhost:5001/api/auth/register" -Body @{
        email = $driverEmail
        password = $password
        firstName = "Driver"
        lastName = "Smoke"
        phoneNumber = "+381600000001"
        role = "Driver"
    })

    [void](Invoke-JsonPost -Uri "http://localhost:5001/api/auth/register" -Body @{
        email = $passengerEmail
        password = $password
        firstName = "Passenger"
        lastName = "Smoke"
        phoneNumber = "+381600000002"
        role = "Passenger"
    })

    Write-Host "[STEP] Logging in users"
    $driverLogin = Invoke-JsonPost -Uri "http://localhost:5001/api/auth/login" -Body @{
        email = $driverEmail
        password = $password
    }

    $passengerLogin = Invoke-JsonPost -Uri "http://localhost:5001/api/auth/login" -Body @{
        email = $passengerEmail
        password = $password
    }

    $driverToken = $driverLogin.accessToken
    $passengerToken = $passengerLogin.accessToken

    if ([string]::IsNullOrWhiteSpace($driverToken) -or [string]::IsNullOrWhiteSpace($passengerToken)) {
        throw "Failed to retrieve JWT tokens from login responses."
    }

    Write-Host "[STEP] Creating ride (validates Ride -> User gRPC)"
    $ride = Invoke-JsonPost -Uri "http://localhost:5002/api/rides" -Headers @{ Authorization = "Bearer $driverToken" } -Body @{
        originName = "Belgrade"
        originLatitude = 44.7866
        originLongitude = 20.4489
        destinationName = "Novi Sad"
        destinationLatitude = 45.2671
        destinationLongitude = 19.8335
        departureTime = (Get-Date).AddHours(2).ToString("o")
        availableSeats = 3
        pricePerSeat = 10
        currency = "RSD"
        autoConfirmBookings = $true
    }

    $rideId = [string]$ride.id
    if ([string]::IsNullOrWhiteSpace($rideId)) {
        throw "Ride creation did not return an id."
    }

    Write-Host "[INFO] Ride created: $rideId"

    if (-not $SkipDirectGrpcCheck) {
        if (Test-CommandAvailable -CommandName "grpcurl") {
            Write-Host "[STEP] Direct gRPC check to Ride service (grpcurl)"
            $protoPath = Join-Path $ComposeProjectRoot "Services\Ride\Ride.Infrastructure\Protos\ride.proto"
            if (-not (Test-Path $protoPath)) {
                throw "Proto file not found at: $protoPath"
            }

            $payload = "{\"" + "ride_id\"" + ":\"" + $rideId + "\",\"" + "seats_requested\"" + ":1}"
            $grpcOut = & grpcurl -plaintext -import-path $ComposeProjectRoot -proto $protoPath -d $payload localhost:50052 ride.RideGrpc/CheckAvailability
            Write-Host $grpcOut
        }
        else {
            Write-Host "[WARN] grpcurl not found. Skipping direct gRPC check."
        }
    }

    Write-Host "[STEP] Creating booking (validates Booking -> User and Booking -> Ride gRPC)"
    $booking = Invoke-JsonPost -Uri "http://localhost:5003/api/bookings" -Headers @{ Authorization = "Bearer $passengerToken" } -Body @{
        rideId = $rideId
        seatsToBook = 1
    }

    $bookingId = [string]$booking.id
    if ([string]::IsNullOrWhiteSpace($bookingId)) {
        throw "Booking creation did not return an id."
    }

    Write-Host "[INFO] Booking created: $bookingId"

    Write-Host "[STEP] Fetching ride to verify seats were updated"
    $rideAfter = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/$rideId"
    if ($rideAfter.availableSeats -ne 2) {
        throw "Expected availableSeats=2 after booking, got $($rideAfter.availableSeats)."
    }

    if ($IncludeCancellationCheck) {
        Write-Host "[STEP] Cancelling booking (validates Booking -> Ride ReleaseSeats gRPC)"
        Invoke-RestMethod -Method Put -Uri "http://localhost:5003/api/bookings/$bookingId/cancel" `
            -Headers @{ Authorization = "Bearer $passengerToken" } `
            -ContentType "application/json" `
            -Body (@{ reason = "Smoke test cancellation" } | ConvertTo-Json) | Out-Null

        Write-Host "[STEP] Fetching ride to verify seats were restored after cancellation"
        $rideAfterCancellation = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/$rideId"
        if ($rideAfterCancellation.availableSeats -ne 3) {
            throw "Expected availableSeats=3 after cancellation, got $($rideAfterCancellation.availableSeats)."
        }
    }

    if ($IncludeRideEndpointChecks) {
        Write-Host "[STEP] Creating second ride for Ride endpoint checks"
        $rideForEndpoints = Invoke-JsonPost -Uri "http://localhost:5002/api/rides" -Headers @{ Authorization = "Bearer $driverToken" } -Body @{
            originName = "Nis"
            originLatitude = 43.3209
            originLongitude = 21.8958
            destinationName = "Kragujevac"
            destinationLatitude = 44.0128
            destinationLongitude = 20.9114
            departureTime = (Get-Date).AddHours(4).ToString("o")
            availableSeats = 2
            pricePerSeat = 15
            currency = "RSD"
            autoConfirmBookings = $true
        }

        $rideEndpointId = [string]$rideForEndpoints.id
        if ([string]::IsNullOrWhiteSpace($rideEndpointId)) {
            throw "Ride endpoint check setup failed: created ride id missing."
        }

        Write-Host "[STEP] Ride GET by id should return scheduled ride"
        $rideById = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/$rideEndpointId"
        if ([string]$rideById.id -ne $rideEndpointId) {
            throw "GET /api/rides/{id} returned unexpected ride id."
        }
        if ([int]$rideById.status -ne 0) {
            throw "Expected ride status Scheduled (0), got $($rideById.status)."
        }

        Write-Host "[STEP] Ride search should include the new ride"
        $search = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/search?origin=Nis&destination=Kragujevac&page=1&pageSize=20"
        $searchHit = $search | Where-Object { [string]$_.id -eq $rideEndpointId }
        if ($null -eq $searchHit) {
            throw "GET /api/rides/search did not return the created ride."
        }

        Write-Host "[STEP] Driver my-rides should include the new ride"
        $myRides = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/my-rides" -Headers @{ Authorization = "Bearer $driverToken" }
        $myRideHit = $myRides | Where-Object { [string]$_.id -eq $rideEndpointId }
        if ($null -eq $myRideHit) {
            throw "GET /api/rides/my-rides did not include the created ride."
        }

        Write-Host "[STEP] Start ride endpoint should transition to InProgress"
        Invoke-RestMethod -Method Put -Uri "http://localhost:5002/api/rides/$rideEndpointId/start" -Headers @{ Authorization = "Bearer $driverToken" } | Out-Null
        $rideAfterStart = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/$rideEndpointId"
        if ([int]$rideAfterStart.status -ne 1) {
            throw "Expected ride status InProgress (1) after start, got $($rideAfterStart.status)."
        }

        Write-Host "[STEP] Complete ride endpoint should transition to Completed"
        Invoke-RestMethod -Method Put -Uri "http://localhost:5002/api/rides/$rideEndpointId/complete" -Headers @{ Authorization = "Bearer $driverToken" } | Out-Null
        $rideAfterComplete = Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/rides/$rideEndpointId"
        if ([int]$rideAfterComplete.status -ne 2) {
            throw "Expected ride status Completed (2) after complete, got $($rideAfterComplete.status)."
        }
    }

    Write-Host "[SUCCESS] Smoke test passed. Ride communication path is working."
    exit 0
}
catch {
    Write-Error "Smoke test failed: $($_.Exception.Message)"
    Write-Host "[DIAG] Last logs (ride-service):"
    docker compose logs ride-service --tail 100
    Write-Host "[DIAG] Last logs (booking-service):"
    docker compose logs booking-service --tail 100
    Write-Host "[DIAG] Last logs (user-service):"
    docker compose logs user-service --tail 100
    exit 1
}
finally {
    Pop-Location
}
