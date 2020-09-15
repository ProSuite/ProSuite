@echo off
@rem Remove untracked files from working tree
@rem (e.g. all obj/ directories). Be careful!

set GIT="git.exe"

%GIT% clean -dx -n

set AREYOUSURE=N
@echo Deleting ALL untracked content from working tree!
set /p AREYOUSURE="Are you sure? (y/N) "
if /i "%AREYOUSURE%" NEQ "y" goto END

%GIT% clean -dx -f

@rem Keep window open to allow reading messages
@pause
:END
