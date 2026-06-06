using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RaffleManager.Models;
using RaffleManager.Services;
using RaffleManager.UI.Components;

namespace RaffleManager.UI;

internal sealed class MainWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly PersistenceService persistence;
    private readonly RaffleService raffle;
    private readonly SoundService sound;
    private readonly LogoService logo;
    private readonly AnnouncementService announcements;
    private readonly Action openSettings;
    private readonly Stopwatch spinWatch = new();

    private string nameInput = string.Empty;
    private string worldInput = string.Empty;
    private int ticketsInput = 1;
    private bool spinning;
    private double nextTickSeconds;
    private string displayName = "Ready";
    private bool displayFlip;
    private WinnerRecord? winnerPopup;
    private Guid? pendingDelete;
    private bool pendingClearWinnerHistory;

    public MainWindow(Configuration config, PersistenceService persistence, RaffleService raffle, SoundService sound, LogoService logo, AnnouncementService announcements, Action openSettings)
        : base("RaffleManager###RaffleManagerMain")
    {
        Size = new Vector2(1160, 740);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(980, 620),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        this.config = config;
        this.persistence = persistence;
        this.raffle = raffle;
        this.sound = sound;
        this.logo = logo;
        this.announcements = announcements;
        this.openSettings = openSettings;
    }

    private VenueProfile Profile => config.Profile;
    private RaffleState Data => Profile.Data;

    public override void PreDraw() => RaffleTheme.Push();
    public override void PostDraw() => RaffleTheme.Pop();
    public void Dispose() { }

    public override void Draw()
    {
        UpdateSpinAnimation();
        DrawHeader();
        ImGui.Separator();

        if (ImGui.BeginTabBar("##mainTabs"))
        {
            if (ImGui.BeginTabItem("Raffle"))
            {
                DrawRaffleTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("History"))
            {
                DrawHistoryTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        DrawWinnerPopup();
        DrawDeleteConfirmation();
        DrawClearHistoryConfirmation();
    }

    private void DrawRaffleTab()
    {
        var avail = ImGui.GetContentRegionAvail();
        var splitterWidth = 8f;
        var layoutWidth = MathF.Max(0f, avail.X - splitterWidth);
        var leftWidth = GetProfileLeftPanelWidth(layoutWidth);

        if (UiHelpers.BeginCard("##leftColumn", new Vector2(leftWidth, avail.Y)))
        {
            DrawAddCard();
            ImGui.Spacing();
            DrawContestantsCard();
        }
        UiHelpers.EndCard();

        ImGui.SameLine(0, 0);
        DrawSplitter(splitterWidth, avail.Y);
        ImGui.SameLine(0, 0);

        if (UiHelpers.BeginCard("##rightColumn", new Vector2(0, avail.Y)))
        {
            DrawRandomizerCard();
        }
        UiHelpers.EndCard();
    }

    private void DrawHistoryTab()
    {
        var records = raffle.WinnerHistory
            .OrderByDescending(w => w.PulledAt)
            .ToList();

        UiHelpers.Header("Winner History", $"{records.Count:N0} completed raffle pull(s) for profile '{Profile.Name}'.");
        UiHelpers.TextMutedWrapped("History is saved per venue profile and records winner details at the moment the raffle is pulled.");
        ImGui.Spacing();

        if (records.Count > 0)
        {
            if (ImGui.Button("Delete History"))
                pendingClearWinnerHistory = true;
            UiHelpers.TooltipOnHover("Deletes the saved winner history for only the active venue profile. Current contestants are not removed.");
        }

        ImGui.Spacing();

        if (records.Count == 0)
        {
            if (UiHelpers.BeginCard("##emptyHistory", new Vector2(0, 110f)))
            {
                ImGui.TextColored(RaffleTheme.Teal, "No previous winners yet.");
                UiHelpers.TextMutedWrapped("After you pull a winner from the Raffle tab, the result will be saved here with the winner, tickets, draw size, and date.");
            }
            UiHelpers.EndCard();
            return;
        }

        var tableHeight = MathF.Max(160f, ImGui.GetContentRegionAvail().Y);
        if (ImGui.BeginTable("##winnerHistory", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new Vector2(0, tableHeight)))
        {
            ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn("Winner", ImGuiTableColumnFlags.WidthStretch, 2.0f);
            ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthStretch, 1.0f);
            ImGui.TableSetupColumn("Tickets", ImGuiTableColumnFlags.WidthFixed, 82f);
            ImGui.TableSetupColumn("Draw Tickets", ImGuiTableColumnFlags.WidthFixed, 105f);
            ImGui.TableSetupColumn("Participants", ImGuiTableColumnFlags.WidthFixed, 96f);
            ImGui.TableHeadersRow();

            foreach (var record in records)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); UiHelpers.ClippedTextWithTooltip(record.PulledAt.ToString("yyyy-MM-dd HH:mm"));
                ImGui.TableNextColumn(); UiHelpers.ClippedTextWithTooltip(record.Name);
                ImGui.TableNextColumn(); UiHelpers.ClippedTextWithTooltip(string.IsNullOrWhiteSpace(record.World) ? "—" : record.World);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(record.Tickets.ToString("N0"));
                ImGui.TableNextColumn(); ImGui.TextUnformatted(record.TotalTickets.ToString("N0"));
                ImGui.TableNextColumn(); ImGui.TextUnformatted(record.TotalParticipants.ToString("N0"));
            }

            ImGui.EndTable();
        }
    }

    private float GetProfileLeftPanelWidth(float layoutWidth)
    {
        const float minLeft = 380f;
        const float minRight = 420f;

        if (layoutWidth <= minLeft + minRight)
            return MathF.Max(minLeft, layoutWidth * 0.40f);

        var ratio = Profile.MainWindowLeftPanelRatio <= 0f ? 0.40f : Profile.MainWindowLeftPanelRatio;
        return Math.Clamp(layoutWidth * ratio, minLeft, layoutWidth - minRight);
    }

    private void DrawSplitter(float width, float height)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, RaffleTheme.Border);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RaffleTheme.Pink);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, RaffleTheme.Teal);
        ImGui.Button("##leftRightSplitter", new Vector2(width, height));
        ImGui.PopStyleColor(3);

        if (ImGui.IsItemActive())
        {
            var contentWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
            var layoutWidth = MathF.Max(1f, contentWidth - width);
            var currentLeftWidth = GetProfileLeftPanelWidth(layoutWidth);
            var newLeftWidth = Math.Clamp(currentLeftWidth + ImGui.GetIO().MouseDelta.X, 380f, MathF.Max(380f, layoutWidth - 420f));
            Profile.MainWindowLeftPanelRatio = Math.Clamp(newLeftWidth / layoutWidth, 0.20f, 0.80f);
        }

        if (ImGui.IsItemDeactivated())
            persistence.SaveData();
        if (ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }

    private void DrawHeader()
    {
        ImGui.TextColored(RaffleTheme.Pink, "RaffleManager");
        ImGui.SameLine();
        UiHelpers.TextMuted($"  {Profile.VenueName}  |  Profile: {Profile.Name}");

        const float settingsWidth = 94f;
        var supportWidth = UiHelpers.GetSupportButtonWidth();
        var toolbarWidth = settingsWidth + ImGui.GetStyle().ItemSpacing.X + supportWidth;
        var avail = ImGui.GetContentRegionAvail().X;
        if (avail > toolbarWidth)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - toolbarWidth);
        if (ImGui.Button("Settings", new Vector2(settingsWidth, 0))) openSettings();
        ImGui.SameLine();
        UiHelpers.DrawSupportButton("main-support", supportWidth);
    }

    private void DrawAddCard()
    {
        UiHelpers.Header("Add Contestant", "Manual entry or target a player and add their ticket purchase. Matching name and world adds to the existing ticket count.");

        ImGui.Text("Character Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##name", ref nameInput, 64);
        ImGui.Text("World");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##world", ref worldInput, 64);
        ImGui.Text("Number of Tickets");
        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt("##tickets", ref ticketsInput, 1, 10);
        ticketsInput = Math.Max(1, ticketsInput);

        DrawQuantityButtons();

        if (ImGui.Button("Add to Raffle"))
        {
            if (raffle.AddOrUpdate(nameInput, worldInput, ticketsInput))
            {
                nameInput = string.Empty;
                worldInput = string.Empty;
                ticketsInput = 1;
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Add Target"))
        {
            if (raffle.AddCurrentTarget(ticketsInput))
                ticketsInput = 1;
        }
        UiHelpers.TooltipOnHover("Uses your current in-game target's name and home world. If that player already exists with the same name/world, their tickets are increased.");
        ImGui.SameLine();
        if (ImGui.Button("Undo")) raffle.Undo();

        UiHelpers.TextMutedWrapped(raffle.LastStatus);
    }

    private void DrawQuantityButtons()
    {
        if (ImGui.SmallButton("+1")) ticketsInput = 1;
        ImGui.SameLine();
        if (ImGui.SmallButton("+5")) ticketsInput = 5;
        ImGui.SameLine();
        if (ImGui.SmallButton("+10")) ticketsInput = 10;
        ImGui.SameLine();
        if (ImGui.SmallButton("+25")) ticketsInput = 25;
        ImGui.SameLine();
        if (ImGui.SmallButton("Reset")) ticketsInput = 1;
        ticketsInput = Math.Max(1, ticketsInput);
    }

    private void DrawContestantsCard()
    {
        ImGui.Spacing();
        UiHelpers.Header("Contestants", $"{raffle.ParticipantCount:N0} contestant(s) · {raffle.TotalTickets:N0} ticket(s)");
        if (ImGui.BeginTable("##entries", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new Vector2(0, -42f)))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 2.8f);
            ImGui.TableSetupColumn("Tickets", ImGuiTableColumnFlags.WidthFixed, 78f);
            ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthStretch, 1.0f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 62f);
            ImGui.TableHeadersRow();

            foreach (var entry in raffle.Entries.ToList())
            {
                ImGui.PushID(entry.Id.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); UiHelpers.ClippedTextWithTooltip(entry.Name);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.Tickets.ToString("N0"));
                ImGui.TableNextColumn(); UiHelpers.TextMuted(string.IsNullOrWhiteSpace(entry.World) ? "—" : entry.World);
                ImGui.TableNextColumn();
                if (ImGui.SmallButton("Delete")) pendingDelete = entry.Id;
                ImGui.PopID();
            }
            ImGui.EndTable();
        }

        if (ImGui.Button("Clear All")) ImGui.OpenPopup("Clear all contestants?");
        if (ImGui.BeginPopupModal("Clear all contestants?", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped("Remove every contestant from this raffle?");
            if (ImGui.Button("Clear", new Vector2(120, 0))) { raffle.Clear(); ImGui.CloseCurrentPopup(); }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void DrawRandomizerCard()
    {
        UiHelpers.Header("Raffle Randomizer", "Each ticket is one chance in the draw. A player with 100 tickets has ten times as many chances as a player with 10 tickets.");
        DrawJackpotStrip();

        ImGui.Spacing();
        var cardSize = new Vector2(0, 300f);
        if (UiHelpers.BeginCard("##spinner", cardSize))
        {
            DrawSpinnerContents();
        }
        UiHelpers.EndCard();

        ImGui.Spacing();
        var buttonLabel = spinning ? "Picking..." : "Pick Random Winner";
        var buttonWidth = 260f;
        var x = (ImGui.GetContentRegionAvail().X - buttonWidth) * 0.5f;
        if (x > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + x);
        if (!spinning && ImGui.Button(buttonLabel, new Vector2(buttonWidth, 42f))) StartSpin();

    }

    private void DrawJackpotStrip()
    {
        var width = ImGui.GetContentRegionAvail().X;
        var compact = width < 720f;
        var stripHeight = compact ? 174f : 112f;

        if (UiHelpers.BeginCard("##jackpotStrip", new Vector2(0, stripHeight)))
        {
            if (compact)
            {
                if (ImGui.BeginTable("##jackpotCompact", 2, ImGuiTableFlags.SizingStretchSame))
                {
                    DrawMetricCell("Base Jackpot", UiHelpers.Gil(Profile.BaseJackpot));
                    DrawMetricCell("Ticket Price", UiHelpers.Gil(Profile.TicketPrice));
                    DrawMetricCell("Total Tickets", raffle.TotalTickets.ToString("N0"));
                    DrawMetricCell("Total Jackpot", UiHelpers.Gil(raffle.Jackpot));
                    DrawMetricCell($"Winner {Profile.WinnerSplitPercent}%", UiHelpers.Gil(raffle.WinnerPayout));
                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.Columns(5, "##jackpotCols", false);
                DrawMetric("Base Jackpot", UiHelpers.Gil(Profile.BaseJackpot));
                ImGui.NextColumn();
                DrawMetric("Ticket Price", UiHelpers.Gil(Profile.TicketPrice));
                ImGui.NextColumn();
                DrawMetric("Total Tickets", raffle.TotalTickets.ToString("N0"));
                ImGui.NextColumn();
                DrawMetric("Total Jackpot", UiHelpers.Gil(raffle.Jackpot));
                ImGui.NextColumn();
                DrawMetric($"Winner {Profile.WinnerSplitPercent}%", UiHelpers.Gil(raffle.WinnerPayout));
                ImGui.Columns(1);
            }
        }
        UiHelpers.EndCard();
    }

    private static void DrawMetric(string label, string value)
    {
        UiHelpers.TextMutedWrapped(label);
        ImGui.PushStyleColor(ImGuiCol.Text, RaffleTheme.Teal);
        ImGui.TextWrapped(value);
        ImGui.PopStyleColor();
    }

    private static void DrawMetricCell(string label, string value)
    {
        ImGui.TableNextColumn();
        DrawMetric(label, value);
        ImGui.Spacing();
    }

    private void DrawSpinnerContents()
    {
        var size = ImGui.GetContentRegionAvail();
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var rowWidth = size.X;
        var drawBothLogos = rowWidth >= 760f;
        var drawOneLogo = !drawBothLogos && rowWidth >= 620f;

        // Hide logos entirely at the smallest sizes so the picker always remains visible and centered.
        var logoSize = Math.Clamp(size.Y * 0.68f, 136f, 205f);
        var logoCount = drawBothLogos ? 2f : drawOneLogo ? 1f : 0f;
        var displayHeight = Math.Clamp((logoCount > 0f ? logoSize : size.Y * 0.42f) * 0.66f, 92f, 126f);
        var maxDisplayWidth = drawBothLogos ? 380f : drawOneLogo ? 500f : MathF.Max(320f, rowWidth - 32f);
        var minDisplayWidth = Math.Min(rowWidth - 24f, logoCount > 0f ? 260f : 300f);
        var availableDisplayWidth = rowWidth - (logoSize * logoCount) - (spacing * MathF.Max(0f, logoCount));
        var displayWidth = Math.Clamp(availableDisplayWidth, minDisplayWidth, maxDisplayWidth);
        var totalWidth = (logoSize * logoCount) + displayWidth + (spacing * MathF.Max(0f, logoCount));
        var rowHeight = MathF.Max(logoCount > 0f ? logoSize : displayHeight, displayHeight);
        var y = ImGui.GetCursorPosY() + ((size.Y - rowHeight) * 0.45f);
        ImGui.SetCursorPosY(y);

        var offsetX = (rowWidth - totalWidth) * 0.5f;
        if (offsetX > 0)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

        if (logoCount > 0f)
        {
            DrawLogoOrPlaceholder(new Vector2(logoSize, logoSize));
            ImGui.SameLine();
        }

        var displayYOffset = logoCount > 0f ? (logoSize - displayHeight) * 0.5f : 0f;
        if (displayYOffset > 0)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + displayYOffset);

        var displayMin = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        var displayMax = displayMin + new Vector2(displayWidth, displayHeight);
        draw.AddRectFilled(displayMin, displayMax, ImGui.GetColorU32(RaffleTheme.InputBg), 12f);
        draw.AddRect(displayMin, displayMax, ImGui.GetColorU32(RaffleTheme.Border), 12f, 0, 2f);
        var textColor = ImGui.GetColorU32(displayFlip ? RaffleTheme.Pink : RaffleTheme.Teal);
        var shownText = FitText(displayName, displayWidth - 24f);
        var textSize = ImGui.CalcTextSize(shownText);
        draw.AddText(displayMin + new Vector2((displayWidth - textSize.X) * 0.5f, (displayHeight - textSize.Y) * 0.5f), textColor, shownText);
        ImGui.Dummy(new Vector2(displayWidth, displayHeight));

        if (displayYOffset > 0)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - displayYOffset);

        if (drawBothLogos)
        {
            ImGui.SameLine();
            DrawLogoOrPlaceholder(new Vector2(logoSize, logoSize));
        }
    }

    private static string FitText(string text, float maxWidth)
    {
        if (ImGui.CalcTextSize(text).X <= maxWidth) return text;
        const string suffix = "...";
        for (var len = text.Length - 1; len > 1; len--)
        {
            var candidate = text[..len] + suffix;
            if (ImGui.CalcTextSize(candidate).X <= maxWidth) return candidate;
        }
        return suffix;
    }

    private void DrawLogoOrPlaceholder(Vector2 boxSize)
    {
        var min = ImGui.GetCursorScreenPos();
        var max = min + boxSize;
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(min, max, ImGui.GetColorU32(RaffleTheme.InputBg), 12f);
        draw.AddRect(min, max, ImGui.GetColorU32(RaffleTheme.Border), 12f, 0, 2f);

        var texture = logo.Texture;
        if (texture is not null)
        {
            var imageSize = FitImageSize(texture.Width, texture.Height, boxSize - new Vector2(8f, 8f));
            var imagePos = min + ((boxSize - imageSize) * 0.5f);
            draw.AddImage(texture.Handle, imagePos, imagePos + imageSize);
            ImGui.Dummy(boxSize);
            return;
        }

        var text = string.IsNullOrWhiteSpace(Profile.VenueName) ? "RM" : Profile.VenueName[..Math.Min(2, Profile.VenueName.Length)].ToUpperInvariant();
        var textSize = ImGui.CalcTextSize(text);
        draw.AddText(min + new Vector2((boxSize.X - textSize.X) * 0.5f, (boxSize.Y - textSize.Y) * 0.5f), ImGui.GetColorU32(RaffleTheme.Pink), text);
        ImGui.Dummy(boxSize);
    }

    private static Vector2 FitImageSize(int textureWidth, int textureHeight, Vector2 maxSize)
    {
        if (textureWidth <= 0 || textureHeight <= 0)
            return maxSize;

        var source = new Vector2(textureWidth, textureHeight);
        var scale = MathF.Min(maxSize.X / source.X, maxSize.Y / source.Y);
        // Allow a little upscaling for small logo files, but cap it so they do not become overly blurry/pixelated.
        scale = MathF.Min(scale, 1.50f);
        return new Vector2(MathF.Max(1f, source.X * scale), MathF.Max(1f, source.Y * scale));
    }

    private void StartSpin()
    {
        if (raffle.TotalTickets <= 0)
        {
            DalamudServices.ChatGui.Print("Add contestants before pulling a winner.", "RaffleManager");
            return;
        }
        spinning = true;
        displayName = "...";
        nextTickSeconds = 0;
        sound.Prepare();
        sound.PlayTick();
        spinWatch.Restart();
    }

    private void UpdateSpinAnimation()
    {
        if (!spinning) return;
        var elapsed = spinWatch.Elapsed.TotalSeconds;
        if (elapsed >= 8.0)
        {
            spinning = false;
            spinWatch.Stop();
            var winner = raffle.PullWinner();
            if (winner is not null)
            {
                displayName = winner.DisplayName;
                winnerPopup = winner;
                announcements.AnnounceWinner(winner);
            }
            return;
        }

        if (elapsed < nextTickSeconds) return;
        var entry = raffle.PickRandomTicketOwner();
        if (entry is not null)
        {
            displayName = entry.DisplayName;
            displayFlip = !displayFlip;
            sound.PlayTick();
        }

        var delay = elapsed switch
        {
            < 4.0 => 0.05,
            < 7.0 => 0.10,
            < 9.0 => 0.20,
            _ => 0.50,
        };
        nextTickSeconds = elapsed + delay;
    }

    private void DrawWinnerPopup()
    {
        if (winnerPopup is null) return;

        ImGui.OpenPopup("Winner Announcement###WinnerPopup");

        var viewport = ImGui.GetMainViewport();
        var popupWidth = Math.Clamp(viewport.WorkSize.X * 0.68f, 980f, 1260f);
        var popupSize = new Vector2(popupWidth, popupWidth * 9f / 16f);
        ImGui.SetNextWindowSize(popupSize, ImGuiCond.Always);
        if (ImGui.BeginPopupModal("Winner Announcement###WinnerPopup", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            DrawWinnerPopupContents(popupSize);
            ImGui.EndPopup();
        }
    }

    private void DrawWinnerPopupContents(Vector2 popupSize)
    {
        if (winnerPopup is null) return;

        var draw = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var content = ImGui.GetContentRegionAvail();
        var max = min + content;

        draw.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.08f, 0.03f, 0.14f, 1f)), 14f);
        draw.AddRect(min, max, ImGui.GetColorU32(RaffleTheme.Pink), 14f, 0, 3f);
        draw.AddRect(min + new Vector2(8f, 8f), max - new Vector2(8f, 8f), ImGui.GetColorU32(RaffleTheme.Border), 12f, 0, 1.5f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 22f);
        CenteredText(Profile.VenueName, RaffleTheme.Muted);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
        CenteredText("WE HAVE A WINNER", RaffleTheme.Pink);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 18f);
        var logoBox = Math.Clamp(content.Y * 0.58f, 240f, 320f);
        var winnerTextWidth = Math.Clamp(content.X * 0.28f, 260f, 340f);
        var rowWidth = logoBox + ImGui.GetStyle().ItemSpacing.X + winnerTextWidth + ImGui.GetStyle().ItemSpacing.X + logoBox;
        var rowX = (content.X - rowWidth) * 0.5f;
        if (rowX > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rowX);

        DrawLogoOrPlaceholder(new Vector2(logoBox, logoBox));
        ImGui.SameLine();

        var textBoxMin = ImGui.GetCursorScreenPos();
        var textBoxSize = new Vector2(winnerTextWidth, logoBox);
        draw.AddRectFilled(textBoxMin, textBoxMin + textBoxSize, ImGui.GetColorU32(RaffleTheme.InputBg), 12f);
        draw.AddRect(textBoxMin, textBoxMin + textBoxSize, ImGui.GetColorU32(RaffleTheme.Teal), 12f, 0, 2f);

        var winnerName = FitText(winnerPopup.DisplayName, winnerTextWidth - 30f);
        var nameSize = ImGui.CalcTextSize(winnerName);
        draw.AddText(textBoxMin + new Vector2((winnerTextWidth - nameSize.X) * 0.5f, (logoBox - nameSize.Y) * 0.40f), ImGui.GetColorU32(RaffleTheme.Teal), winnerName);

        var ticketText = $"{winnerPopup.Tickets:N0} of {winnerPopup.TotalTickets:N0} tickets";
        var ticketSize = ImGui.CalcTextSize(ticketText);
        draw.AddText(textBoxMin + new Vector2((winnerTextWidth - ticketSize.X) * 0.5f, (logoBox - ticketSize.Y) * 0.64f), ImGui.GetColorU32(RaffleTheme.Muted), ticketText);
        ImGui.Dummy(textBoxSize);

        ImGui.SameLine();
        DrawLogoOrPlaceholder(new Vector2(logoBox, logoBox));

        var payoutY = MathF.Max(ImGui.GetCursorPosY() + 16f, popupSize.Y - 138f);
        ImGui.SetCursorPosY(payoutY);
        var payoutLine = $"Winner Payout: {UiHelpers.Gil(winnerPopup.Payout)}";
        CenteredText(payoutLine, RaffleTheme.Teal);
        CenteredText($"Total Jackpot: {UiHelpers.Gil(winnerPopup.Jackpot)}  ·  Split: {winnerPopup.SplitPercent}%", RaffleTheme.Muted);

        var closeWidth = 150f;
        var closeHeight = 34f;
        ImGui.SetCursorPos(new Vector2((popupSize.X - closeWidth) * 0.5f, popupSize.Y - 58f));
        if (ImGui.Button("Close", new Vector2(closeWidth, closeHeight)))
        {
            winnerPopup = null;
            ImGui.CloseCurrentPopup();
        }
    }

    private static void CenteredText(string text, Vector4 color)
    {
        var width = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(text);
        var offset = (width - textSize.X) * 0.5f;
        if (offset > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextColored(color, text);
    }

    private void DrawClearHistoryConfirmation()
    {
        if (!pendingClearWinnerHistory) return;
        ImGui.OpenPopup("Delete winner history?");
        if (ImGui.BeginPopupModal("Delete winner history?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize))
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 34f);
            ImGui.TextWrapped($"Delete all saved winner history for the '{Profile.Name}' venue profile? Current contestants will not be removed.");
            ImGui.PopTextWrapPos();
            ImGui.Spacing();

            if (ImGui.Button("Delete History", new Vector2(140, 0)))
            {
                raffle.ClearWinnerHistory();
                pendingClearWinnerHistory = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                pendingClearWinnerHistory = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private void DrawDeleteConfirmation()
    {
        if (pendingDelete is null) return;
        ImGui.OpenPopup("Delete contestant?");
        if (ImGui.BeginPopupModal("Delete contestant?", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped("Remove this contestant from the raffle?");
            if (ImGui.Button("Delete", new Vector2(120, 0)))
            {
                raffle.Remove(pendingDelete.Value);
                pendingDelete = null;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                pendingDelete = null;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}
