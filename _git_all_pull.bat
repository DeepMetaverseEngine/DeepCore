@echo off
echo "------------------------ PULL ------------------------"
git pull "origin"  master:master
git config lfs.allowincompletepush false
git lfs fetch --all origin
git lfs pull
echo "------------------------ DONE ------------------------"

pause

