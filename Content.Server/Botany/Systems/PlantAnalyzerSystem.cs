using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Server.Botany.Components;
using Robust.Shared.Utility;
using Content.Shared.Examine;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        SeedData? seed = null;

        if (TryComp(args.Target, out PlantHolderComponent? plantHolder))
        {
            if (plantHolder.Seed == null)
            {
                _popup.PopupCursor(Loc.GetString("plant-analyzer-no-plant-in-tray",
                    ("trayName", Comp<MetaDataComponent>((EntityUid) args.Target).EntityName)),
                    args.User, PopupType.Medium);

                return;
            }
            seed = plantHolder.Seed;
        }

        if (TryComp(args.Target, out SeedComponent? seedComp))
        {
            Log.Info("yes we did get here.");
            if (_botany.TryGetSeed(seedComp, out SeedData? seedFromComp))
                seed = seedFromComp;
        }

        if (seed == null)
            return;

        var msg = new FormattedMessage();

        if (plantHolder != null)
        {
            msg.AddMarkupPermissive(Loc.GetString("plant-analyzer-scan-preamble-plant",
            ("plantName", Loc.GetString(seed.DisplayName))));
        }
        else
        {
            msg.AddMarkupPermissive(Loc.GetString("plant-analyzer-scan-preamble-seeds",
            ("seedName", Loc.GetString(seed.Name)),
            ("seedNoun", Loc.GetString(seed.Noun))));
        }

        msg.PushNewline();
        msg.PushNewline();
        msg.AddMarkupPermissive(Loc.GetString($"plant-analyzer-yield",
            ("yield", seed.Yield)));
        msg.PushNewline();
        msg.AddMarkupPermissive(Loc.GetString($"plant-analyzer-potency",
            ("potency", (int) seed.Potency)));
        msg.PushNewline();

        if (plantHolder != null)
        {
            msg.AddMarkupPermissive(Loc.GetString($"plant-analyzer-health",
            ("health", (int) plantHolder.Health)));
            msg.PushNewline();

            if (!seed.Viable)
            {
                msg.AddMarkupPermissive(Loc.GetString("plant-analyzer-seeds-unviable"));
                msg.PushNewline();
            }
        }

        _examineSystem.SendExamineTooltip(args.User, (EntityUid) args.Target, msg, false, false);
    }
}
