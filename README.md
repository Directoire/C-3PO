<div id="top"></div>

[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]



<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/Directoire/C-3PO">
    <img src="https://cdn.discordapp.com/avatars/921405951116972033/44725cc35687495594abe6e02608aac9.png?size=1024" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">C-3PO</h3>

  <p align="center">
    A Star Wars themed Discord bot with an interactive onboarding process
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
            <ul>
              <li><a href="#configuration">Configuration</a></li>
    </ul>
    </li>
    <li><a href="#license">License</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

<br />
<div align="center">
<img src="https://github.com/Directoire/C-3PO/blob/master/assets/showcase.gif?raw=true">
</div>
<br />

Hello there! This is a Discord bot I originally wrote for a Discord community I managed. This bot has simple moderation features and has an interactive onboarding process, which gives new members a unique and fun entrance to a server.


<!-- GETTING STARTED -->
## Getting Started

1. Clone the repo
   ```sh
   git clone https://github.com/Directoire/C-3PO.git
   ```
2. Configure `appsettings.json` and `appsettings.Development.json`
3. Run the bot
    ```
    dotnet run --project .\C-3PO\C-3PO.csproj --configuration <debug|release>
    ```

### Configuration

| Key          | Type   | Description                                                                                                                                                                         |
|--------------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Database     | string | The connection string of your MySQL database.                                                                                                                                       |
| Token        | string | The token of your Discord bot.                                                                                                                                                      |
| Guild        | ulong  | The ID of the Discord server which C-3PO will run in.                                                                                                                               |
| Hangar       | ulong  | The ID of the Discord channel where new members will be announced.                                                                                                                  |
| Rules        | ulong  | The ID of the Discord channel where the rules are located.                                                                                                                          |
| Logs         | ulong  | The ID of the Discord channel where the logs should be send to.                                                                                                                     |
| Conduct      | ulong  | The ID of the Discord channel where the rules should be send to.                                                                                                                    |
| LoadingBay   | ulong  | The ID of the Discord channel where members can subscribe to categories and their respective notification roles.                                                                    |
| Support      | ulong  | The ID of the Discord channel where a thread is automatically created for programming questions.                                                                                    |
| OuterRim     | ulong  | The ID of the Discord category where a channel will be created for the onboarding process of a new member.                                                                          |
| Onboarding   | ulong  | The ID of the Discord role that is assigned to members that are in the onboarding stage.                                                                                            |
| Ejected      | ulong  | The ID of the Discord role that is assigned to members that are kicked, banned or failed the onboarding process.                                                                    |
| Civilian     | ulong  | The ID of the Discord role that is assigned to members that have passed the onboarding process.                                                                                     |
| Unidentified | ulong  | The ID of the Discord role that is assigned to members that join during a lockdown. Once the lockdown is over, these members will automatically be put into the onboarding process. |
| HeartbeatUrl | string | A heartbeat URL that can be used for monitoring purposes.                                                                                                                           |

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.


<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Discord.NET](https://github.com/discord-net/Discord.Net)
* [Discord.Addons.Hosting](https://github.com/Hawxy/Discord.Addons.Hosting)
* [.NET](https://dotnet.microsoft.com)


[forks-shield]: https://img.shields.io/github/forks/Directoire/C-3PO.svg?style=flat
[forks-url]: https://github.com/Directoire/C-3PO/network/members
[stars-shield]: https://img.shields.io/github/stars/Directoire/C-3PO.svg?style=flat
[stars-url]: https://github.com/Directoire/C-3PO/stargazers
[license-shield]: https://img.shields.io/github/license/Directoire/C-3PO.svg?style=flat
[license-url]: https://github.com/Directoire/C-3PO/blob/master/LICENSE
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=flat&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/hendrik-demir
