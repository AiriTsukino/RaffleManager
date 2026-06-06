# RaffleManager

RaffleManager is a Dalamud plugin for Final Fantasy XIV venues. It manages ticket-based raffles, live jackpot calculations, venue profiles, winner history, branded winner screenshots, and optional in-game winner announcements.

Repo Link: `https://raw.githubusercontent.com/AiriTsukino/RaffleManager/main/pluginmaster.json`

## Features

- Multiple isolated venue profiles.
- Ticket-based raffle entries where each ticket counts as one chance to win.
- Configurable ticket price.
- Configurable base jackpot.
- Live total jackpot and winner payout display.
- Configurable venue split ratio such as 50/50, 60/40, 70/30, and more.
- Add contestants manually or from current target.
- Automatically updates existing contestants when the same player and world are added again.
- Custom venue name and custom logo support for branded winner screenshots.
- Winner history per venue profile.
- Import and export venue profiles.
- Optional winner announcement to `/say`, `/shout`, or `/yell`.
- Configurable winner announcement message.
- Tick sound during winner selection with volume control.
- `/rafflemanager` toggles the main UI.
- `/rafflemanagersettings` toggles settings.

## Commands

- `/rafflemanager` opens or closes the main window.
- `/rafflemanagersettings` opens or closes settings.

## First-time setup in game

1. Run `/rafflemanagersettings` to configure the plugin and `/rafflemanager` to open the main window.
2. Create or rename a venue profile in **Venue Profiles**.
3. Set your venue name, logo, ticket price, base jackpot, and split ratio.
4. Add contestants manually or by targeting a player and entering their ticket amount.
5. Review the live jackpot and winner payout before drawing.
6. Press **Pick Random Winner** to select a winner.
7. Use the **History** tab to review previous winners or clear history when needed.

## Notes

- Each ticket is treated as one raffle entry.
- A player with 100 tickets has ten times the chance of a player with 10 tickets.
- Venue profiles keep their own settings, contestants, branding, and history.
- Custom logos should use a clear square image for best results in the main window and winner popup.