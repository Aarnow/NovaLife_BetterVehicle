using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Helper.VehicleHelper.Classes;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.Utils;
using System;
using System.Linq;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace BetterVehicle
{
    public class BetterVehicle : ModKit.ModKit
    {
        public BetterVehicle(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InsertMenu();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        public void InsertMenu()
        {
            _menu.AddAdminTabLine(PluginInformations, 5, "BetterVehicle", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                BetterVehiclePanel(player);
            });
        }

        public async void BetterVehiclePanel(Player player)
        {
            var query = await VehicleHelper.GetVehicles();
            
            Panel panel = PanelHelper.Create("BetterVehicle- Liste des véhicules", UIPanel.PanelType.TabPrice, player, () => BetterVehiclePanel(player));

            if (query != null && query.Count > 0)
            {
                foreach (var vehicle in query)
                {
                    panel.AddTabLine($"{vehicle.Name}", $"{vehicle.Price}€", VehicleUtils.GetIconId(vehicle.ModelId), ui =>
                    {
                        SetVehiclePrice(player, vehicle);
                    });
                }
                panel.NextButton("Modifier", () => panel.SelectTab());
            }
            else
            {
                panel.AddTabLine("Aucun véhicule", _ => { });

                panel.NextButton("Importer", async () =>
                {
                    player.Notify("ModKit", "Nous importons les véhicules. Veuillez patienter un instant.", NotificationManager.Type.Info);

                    foreach (var (model, index) in Nova.v.vehicleModels.Select((model, index) => (model, index)))
                    {
                        if (!model.isDeprecated)
                        {
                            Vehicle vehicle = new Vehicle();
                            vehicle.Name = model.vehicleName;
                            vehicle.ModelId = index;
                            vehicle.Price = 100.00;
                            await vehicle.Save();
                        }
                    }

                    BetterVehiclePanel(player);
                });
            }

            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPanel(player, AAMenu.AAMenu.menu.AdminTabLines);
            });
            panel.CloseButton();

            panel.Display();
        }

        #region SETTERS
        public void SetVehiclePrice(Player player, Vehicle vehicle)
        {
            Panel panel = PanelHelper.Create("BetterVehicle - Modifier le prix", UIPanel.PanelType.Input, player, () => SetVehiclePrice(player, vehicle));

            panel.TextLines.Add("Donner un prix à ce véhicule");
            panel.inputPlaceholder = "exemple: 2500.35";

            panel.PreviousButtonWithAction("Sélectionner", async () =>
            {
                if (double.TryParse(panel.inputText, out double price))
                {
                    if(price > 0)
                    {
                        vehicle.Price = Math.Round(price, 2);
                        if (await vehicle.Save())
                        {
                            player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                            return true;
                        }
                        else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                    } 
                    else player.Notify("BetterVehicle", "Prix invalide (1€ minimum)", NotificationManager.Type.Warning);
                }
                else player.Notify("BetterVehicle", "Format incorrect", NotificationManager.Type.Warning);

                return false;
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion
    }
}