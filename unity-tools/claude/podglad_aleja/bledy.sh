#!/bin/bash
cd "$(dirname "$0")"; dotnet build -c Release -o bin -v q -nologo 2>&1 | grep -E " error " | sed -E 's/ \[[^]]*\]$//; s/^.*\/(gen|Region|podglad_aleja|Runtime|fake)\///; s/\([0-9]+,[0-9]+\)//' | sort | uniq -c | sort -rn
