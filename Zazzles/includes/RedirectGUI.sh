#!/bin/bash
export DISPLAY=:0 > /dev/null 2>&1
$@
exit $?