#!/usr/bin/env bash
set -euo pipefail

awslocal s3 mb s3://vetcare-pets
awslocal sqs create-queue --queue-name appointment-reminders
awslocal sqs create-queue --queue-name appointment-cancellations
