[![licence badge]][licence]
[![stars badge]][stars]
[![forks badge]][forks]
[![issues badge]][issues]

[licence badge]:https://img.shields.io/badge/license-Apache2-blue.svg
[stars badge]:https://img.shields.io/github/stars/koenzomers/RingRecordingDownload.svg
[forks badge]:https://img.shields.io/github/forks/koenzomers/RingRecordingDownload.svg
[issues badge]:https://img.shields.io/github/issues/koenzomers/RingRecordingDownload.svg

[licence]:https://github.com/koenzomers/RingRecordingDownload/blob/master/LICENSE.md
[stars]:https://github.com/koenzomers/RingRecordingDownload/stargazers
[forks]:https://github.com/koenzomers/RingRecordingDownload/network
[issues]:https://github.com/koenzomers/RingRecordingDownload/issues

# Ring Recording Download Tool
Console application written in .NET Core compiled for Windows, Raspberry Pi, Linux and macOS which allows for downloading of Ring recorded events to your local machine. It is just a sample console application wrapper around the [Ring API I have written](https://github.com/KoenZomers/RingApi).

## Version History

2.0.0.0 - February 13, 2020

- Application has been recompiled in .NET Core so it runs on Windows, Linux, Raspberry Pi and macOS devices without any dependencies on OS components

1.1.1.0 - February 12, 2020

- Fixed an issue where it would throw a NullReferenceException after authentication. Thanks to Andre for reporting this!

1.1.0.1 - January 22, 2020

- Upgraded the [Ring API package](https://github.com/KoenZomers/RingApi) to [version 0.4.2.1](https://www.nuget.org/packages/KoenZomers.Ring.Api/0.4.2.1) which fixes an issue retrieving the doorbot history which this tool could run into as well

1.1.0.0 - December 24, 2019

- Ring changed their download API which caused this tool to fail on some recording downloads. Updated the code so that it downloads the recordings properly.
- Added support for using Ring accounts with two factor authentication enabled. If the account you wish to use uses two factor authentication, run the tool manually the first time. It will trigger a text message to be sent to you and ask for it in the console application. Once you've entered the token received in the text message on your phone, it will store the refresh token in the application configuration file so on subsequent runs you don't need to enter it anymore and can run the application unattended in i.e. a daily script. Also for Ring accounts without two factor authentication, you don't have to store the credentials in the config file anymore or provide them on every run as it will utilize the refresh token from the first run to authenticate.
- Upgraded the [Ring API package](https://github.com/KoenZomers/RingApi) to [version 0.4.1.0](https://www.nuget.org/packages/KoenZomers.Ring.Api/0.4.1)

1.0.2.0 - October 27, 2019

- Upgraded the Ring API package to version 0.3.5.0 which ensures the session with the Ring service is still active. This should resolve the 401 Unauthorized errors which occur after one hour of downloading recordings.

1.0.1.0 - October 27, 2019

- Added retry option based on feedback received. The Ring service randomly gives out 404 File Not Found errors. When you just retry it a couple of times it will work. By default it will retry three times now. You can increase this number by providing the -retries flag.
- If an error is returned by the Ring service, the error shown in the console output will now also show the actual error returned by Ring, if available

1.0.0.0 - October 5, 2019

- Initial version

## System Requirements

- Either of: Windows x86, Windows x64, Windows ARM (i.e. Windows 10 IoT), Linux ARM (i.e. Raspberry Pi), Linux x64 (any Linux based distribution), Mac OS (Apple devices)
- For all platforms the application is self containing, so it does not need anything else to be installed on the operating system, not even .NET Core

## Usage Instructions

1. Download the ZIP file of the latest version from [releases](https://github.com/KoenZomers/RingRecordingDownload/releases). Make sure you download the right type for the platform on which you want to run it:
   - Linux ARM (i.e. Raspberry Pi): linux-arm.zip
   - Linux x64 (any Linux based distribution): linux-x64.zip
   - Mac OSX (Apple devices): osx-x64.zip
   - Windows 10 IoT: win-arm.zip
   - Windows: win-x86.zip
2. Extract it to any location on your machine
3. Run RingRecordingDownload.exe in a Command Prompt or PowerShell window to see the possible parameters and samples

![](./Screenshots/CommandLineOptions.png)

![](./Screenshots/SampleExecution.png)

![](./Screenshots/Files.png)

If you want to run this application unattended in i.e. a scheduled daily download script, ensure you run it once manually with your username and password. After this run it will store the refresh token and will run without needing a username, password or two factor authentication token on subsequent runs.

## Current functionality

With this tool in its current state you can:

- Log on once to a two factor authentication enabled Ring account and then have it use the retrieved refresh token to run unattended on subsequent runs
- Download all recordings of the last X days
- Download all recordings between two specific data/times
- Download all recordings of a specific type, i.e. ring or motion

## Feedback

Any kind of feedback is welcome! Feel free to drop me an e-mail at koen@zomers.eu or [create an issue](https://github.com/KoenZomers/RingRecordingDownload/issues).
