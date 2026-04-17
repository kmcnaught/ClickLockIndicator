;// https://github.com/mmikeww/ClickLock-Indicator
#Requires AutoHotkey v1.1.34+
#SingleInstance, Force
CoordMode, Mouse, Screen
SetWinDelay, -1
SetBatchLines, -1

DllCall("SystemParametersInfo", "UInt", 0x101E, "UInt", 0, "UIntP", cl_enabled, "UInt", 0) ;SPI_GETMOUSECLICKLOCK
if (cl_enabled)
   Hotkey, ~LButton, LeftDownHandler, on
else
   MsgBox, % "ClickLock is not enabled in the Control Panel.`n`nExiting."
return

LeftDownHandler()
{
   global tthwnd
   DllCall("SystemParametersInfo", "UInt", 0x2008, "UInt", 0, "UIntP", cl_time, "UInt", 0) ;SPI_GETMOUSECLICKLOCKTIME
   KeyWait, LButton, % "T" cl_time/1000
   if (ErrorLevel) {   ; KeyWait timed out, so button is still held
      DllCall("winmm.dll\PlaySound", AStr, "C:\Windows\Media\Windows Navigation Start.wav", uint, 0, uint, 0)
      Loop, Parse, % "~LButton Up|~RButton|~MButton|~XButton1|~XButton2", |
         Hotkey, %A_LoopField%, ClickLockEnd, on
   }
}

ClickLockEnd()
{
   Loop, Parse, % "~LButton Up|~RButton|~MButton|~XButton1|~XButton2", |
      Hotkey, %A_LoopField%, off
   ; sound on release
   DllCall("winmm.dll\PlaySound", AStr, "C:\Windows\Media\Windows Navigation Start.wav", uint, 0, uint, 0)
}
