// SAFE RESET ACTION
// Clears ONLY per-stream variables.
// Does NOT touch AttendanceHistory or any long-term analytics.

public class CPHInline
{
    public bool Execute()
    {
        // --- Per-stream attendance ---
        CPH.SetGlobalVar("SeenUsers", "{}", true);

        // --- Chat analytics ---
        CPH.SetGlobalVar("analytics.chatMessagesByUser", "{}", true);
        CPH.SetGlobalVar("analytics.chatTotalMessages", 0, true);

        // --- Subs analytics ---
        CPH.SetGlobalVar("analytics.subs", "{}", true);
        CPH.SetGlobalVar("analytics.subsDetailed", "{}", true);
        CPH.SetGlobalVar("analytics.totalSubs", 0, true);

        // --- Follows analytics ---
        CPH.SetGlobalVar("analytics.follows", "{}", true);
        CPH.SetGlobalVar("analytics.totalFollows", 0, true);

        // --- Bits analytics ---
        CPH.SetGlobalVar("analytics.bits", "{}", true);
        CPH.SetGlobalVar("analytics.totalBits", 0, true);

        // --- Raids analytics ---
        CPH.SetGlobalVar("analytics.raids", "[]", true);

        // --- Generated current-session attendance snapshot ---
        CPH.SetGlobalVar("analytics.attendanceSummaryJson", "{}", true);

        // OPTIONAL: Clear final summary (remove if you want to keep last summary)
        // CPH.SetGlobalVar("analytics.finalSummaryJson", "{}", true);

        CPH.LogInfo("SAFE RESET COMPLETE: All per-stream variables cleared. AttendanceHistory preserved.");
        CPH.SendMessage("/me 🔄 Safe Reset Complete! All per-stream data has been cleared without touching long-term attendance history.");

        return true;
    }
}
