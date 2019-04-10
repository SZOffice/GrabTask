@echo off

Set nssm=D:\GrabTask\nssm-2.23\nssm-2.23\win64\nssm.exe
set exe=D:\GrabTask\bin\Debug\GrabTask.exe


%nssm% install "JobsDB ME Notifition" "%exe%"

pause