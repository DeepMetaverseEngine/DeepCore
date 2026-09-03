@ECHO OFF
if not exist ice_db              md ice_db
if not exist ice_db\registry     md ice_db\registry
if not exist ice_db\node_01      md ice_db\node_01
if not exist ice_db\node_02      md ice_db\node_02

start icegridnode      --Ice.Config=.\_icegrid_test_node_01.properties
start icegridnode      --Ice.Config=.\_icegrid_test_node_02.properties
start dotnet tlserver_ice_console.dll _icegrid_test_console.client