"""Shared APK version clock. Both workflows must use the same concurrency group."""
import time

# Seconds since 2020, with a floor above the previous workflow-local counters.
EPOCH = 1577836800
OFFSET = 100000000
ANDROID_MAX = 2100000000


def version_code(now=None):
    value = int(time.time() if now is None else now) - EPOCH + OFFSET
    if not OFFSET <= value <= ANDROID_MAX:
        raise ValueError("APK version clock outside supported Android range")
    return value


if __name__ == "__main__":
    print(version_code())
