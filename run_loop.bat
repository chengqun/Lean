@echo off
setlocal enabledelayedexpansion

REM 计算循环次数
set total=257

REM part.txt 路径
set partfile=E:\Lean\Data\AAshares\part.txt

REM 检查 part.txt 是否存在，不存在则创建并写入0
if not exist "%partfile%" (
    echo 0 > "%partfile%"
)

for /l %%i in (1,1,%total%) do (
    echo 循环 %%i/%total%
    REM 切换到指定目录
    pushd E:\Lean\Launcher\bin\Release

    REM 运行 QuantConnect.Lean.Launcher.exe
    QuantConnect.Lean.Launcher.exe

    REM 返回原目录
    popd

    REM 读取当前数字
    set /p partnum=<"%partfile%"

    REM 数字+1
    set /a partnum=partnum+1

    REM 写回文件
    echo !partnum! > "%partfile%"
)

echo 循环完成
pause
