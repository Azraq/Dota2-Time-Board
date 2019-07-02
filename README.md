# Dota 2 Time Board
A GUI for GSI Based Bot.

## What does it do?

It plays sounds based on your ingame match time. 
Right now following sounds work:
- 5 Minute Bounty Runes
- Healing (minimum 200 HP)(it is random)
- Midas timer


## Todo

- Better GUI (pretty shoddy right now)
- Set custom sounds for different events
- Custom soundboard buttons with hotkeys
- Hotkeys for each sound effect
- Able to toggle sounds on/off
- Save settings in a configuration file

## Credits

[Dota2 GSI](https://github.com/antonpup/Dota2GSI)

[Dota2.AdmiralBulldog.Voice](https://github.com/webmilio/Dota2.AdmiralBulldog.Voice)

All the sounds were taken from internal code or [The Playsound Page](http://chatbot.admiralbulldog.live/playsounds).


## Installation
For Clients/Consumers:

```
Install-Package Dota2GSI

```


For Devs:
```
git clone https://github.com/Azraq/Dota2-Time-Board.git
```
and install missing packages via NuGet Package Manager console
```
Install-Package Dota2GSI
Install-Package Newtonsoft.Json
Install-Package MouseKeyHook
Install-Package NAudio
```
