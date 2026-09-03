cd bin
mysqld  --initialize-insecure --user=root
start mysqld  --defaults-file=%~dp0/my.ini
echo wait 5 sec
ping 127.0.0.1 -n 5 >nul
mysql --port=3307 -uroot <%~dp0\init.sql
mysql --port=3307 -uroot -p123456
