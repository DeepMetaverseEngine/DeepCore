// See https://aka.ms/new-console-template for more information
using DeepCrystal;

Console.WriteLine("Hello, World!");

new RedisLauncher().Start_Redis_EXE(Environment.CurrentDirectory);
new MySQLLauncher().Start_MySQL_EXE(Environment.CurrentDirectory);

Console.In.ReadLine();