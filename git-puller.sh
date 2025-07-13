#!/bin/bash

INTERVAL=5

while true; do
    git pull
    sleep $INTERVAL
done