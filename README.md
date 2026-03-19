> **Warning**
This project is still under development, there may be bugs but the core functionality should be there. You can try it in the [releases tab.](https://codeberg.org/Cautami/YukariLauncher/releases)

# Overview
Yukari is a Windows and Linux launcher project with the purpose of making playing Touhou games (particularly the older ones) as easy as possible in hopes more people try these games. 
It uses an external server (named Ran) that fetches download files for the games listed below, Yukari downloads these files and extracts them for you, letting you hop right in after the download is finished. 

Additionally, some games will support native configuration, meaning you can configure the game settings straight from Yukari itself. As you no longer have to open a separate configuration window, this speeds up the time it takes to configure a game and reduces the reliance on Wine for Linux users. 

> **Warning**
It should be noted that Yukari connects to an external server, however, **no personally identifiable data is transmitted or collected.** This is needed so you can request downloads, which are hosted by me. In the future there may be an opt-in way of synchronizing your data between devices, specifically in the usecase of someone desiring to play on Desktop and their Steam Deck without needing to manually drag the files between devices. (Someone being me)

> **Note**
UI may still be subject to change

![Screenshot showing Yukari with the library fully visible](https://cdn.cautami.dev/yukari_v2_screenshot.png "Yukari Library")

# Progress
## Support for downloading the following:
  ### Mainline
  - [x] th01 ~ *Highly Responsive to Prayers*
  - [x] th02 ~ *the Story of Eastern Wonderland*
  - [x] th03 ~ *Phantasmagoria of Dim.Dream*
  - [x] th04 ~ *Lotus Land Story*
  - [x] th05 ~ *Mystic Square*
  - [x] th06 ~ *Embodiment of Scarlet Devil*
  - [x] th07 ~ *Perfect Cherry Blossom*
  - [x] th08 ~ *Imperishable Night*
  
  ### Spin-Off
  - [x] th07.5 ~ *Immaterial and Missing Power*
  - [x] th10.5 ~ *Scarlet Weather Rhapsody*
  - [x] th12.3 ~ *Touhou Hisoutensoku*
  - [x] th13.5 ~ *Hopeless Masquerade*
  - [x] th14.5 ~ *Urban Legend in Limbo*

Why these games specifically? None of the above are on Steam. 

The goal of Yakumo overall is the preservation and ease of use of playing these games (Particularly PC-98!). <br/>While Yukari will support modern titles, I will not serve these games. 

If you have these games on Steam, Yukari will be capable of detecting them and optionally allowing you to install the patches. 

If you managed to purchase a copy through conventional means elsewhere, you will also be able to have Yukari manually detect them. 

While this may be annoying, and partially go against my goal of the easiest way to play Touhou, I hope you can understand that I personally do not feel right distributing works that are easily accessibly via Steam. Forgive me! (╥ᆺ╥；)

## Support for natively configuring the following:
- [ ] Windows 1st Generation games
    - [ ] Mainline
    - [ ] Spin-off
- [ ] Windows 2nd Generation games
    - [ ] Mainline
    - [ ] Spin-off
- [ ] Windows 3rd Generation games
    - [ ] Mainline
    - [ ] Spin-off
