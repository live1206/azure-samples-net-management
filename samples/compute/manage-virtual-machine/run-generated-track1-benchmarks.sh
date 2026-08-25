#!/usr/bin/env bash
set -euo pipefail

sample_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
export MOCK_ARM_ENDPOINT=${MOCK_ARM_ENDPOINT:-http://127.0.0.1:5050}
export DOTNET_ROLL_FORWARD=${DOTNET_ROLL_FORWARD:-LatestPatch}
benchmark_framework=${BENCHMARK_FRAMEWORK:-net8.0}
server_log=$(mktemp)
server_pid=

cleanup() {
    if [[ -n "${server_pid}" ]]; then
        kill "${server_pid}" 2>/dev/null || true
        wait "${server_pid}" 2>/dev/null || true
    fi
    rm -f "${server_log}"
}
trap cleanup EXIT

dotnet run \
    --project "${sample_dir}/MockArmServer.csproj" \
    --configuration Release \
    >"${server_log}" 2>&1 &
server_pid=$!

for _ in $(seq 1 200); do
    if curl --fail --silent "${MOCK_ARM_ENDPOINT}/__mock/health" >/dev/null 2>&1; then
        break
    fi
    if ! kill -0 "${server_pid}" 2>/dev/null; then
        cat "${server_log}" >&2
        exit 1
    fi
    sleep 0.1
done

if ! curl --fail --silent "${MOCK_ARM_ENDPOINT}/__mock/health" >/dev/null; then
    echo "Mock ARM server did not become ready at ${MOCK_ARM_ENDPOINT}." >&2
    cat "${server_log}" >&2
    exit 1
fi

(
    cd "${sample_dir}"
    dotnet run \
        --project ManageVMSample.GeneratedTrack1Benchmarks.csproj \
        --configuration Release \
        --framework "${benchmark_framework}" \
        -- "$@"
)
