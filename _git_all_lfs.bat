@echo off
echo "------------------------ LFS ------------------------"
git config lfs.allowincompletepush false
git lfs fetch --all origin
git lfs pull
echo "------------------------ DONE ------------------------"
pause

