# Dynamic conditions plugin for acServer

Plugin for acServer adding dynamic conditions online. Written in C#, matches functions of live conditions in Content Manager. Based on [acplugins by Minolin](https://github.com/minolin/acplugins).

# Usage:

Prepare a config like this:

```
# Something to show to remember where config comes from
message=Dynamic conditions for CSP Test Server (Pursuit Mode)

# Plugin settings
plugin.listeningPort=31634
plugin.remotePort=31632
plugin.remoteHostName=127.0.0.1
plugin.serverCapacity=2

# Weather settings
weather.useV2=1
weather.apiKey=  # OpenWeather API key
weather.trackLatitude=51.367222
weather.trackLongitude=0.27511
weather.trackLengthKm=1.929
weather.trackTimezoneId=GMT Standard Time
weather.useRealConditions=0
weather.timeOffset=00:00:00
weather.useFixedStartingTime=0
weather.fixedStartingTimeValue=43200
weather.fixedStartingDateValue=05/02/2022 00:00:00
weather.timeMultiplier=1
weather.temperatureOffset=0
weather.useFixedAirTemperature=0
weather.fixedAirTemperature=25
weather.weatherTypeChangePeriod=00:00:30
weather.weatherTypeChangeToNeighboursOnly=0
weather.weatherRainChance=0.05
weather.weatherThunderChance=0.005
weather.trackGripStartingValue=99
weather.trackGripIncreasePerLap=0.05
weather.trackGripTransfer=80
```

CM can automatically export your settings in this form, or you can set them manually. To check the available settings, look at [ProgramParams.cs](ProgramParams.cs).

After config is ready, simply launch the program and pass params as a command line argument. Alternatively you can append config to the executable: merge executable file with config file, add integer 0xBEE5 and an integer with the size of config file.

You can also provide multiple configs and app would start multiple weather simulations for different servers, but assuming how tiny it is, might be easier to just run a separate plugin for each server.
