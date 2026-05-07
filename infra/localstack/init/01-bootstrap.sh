#!/usr/bin/env bash
set -euo pipefail

awslocal s3 mb s3://vetcare-photos
awslocal sqs create-queue --queue-name vetcare-reminders
