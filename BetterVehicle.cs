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
                        BetterVehicleCreateOrUpdatePanel(player, vehicle);
                    });
                }

                panel.NextButton("Modifier", () => panel.SelectTab());
            }
            else panel.AddTabLine("Aucun véhicule enregistré", _ => { });

            panel.AddButton("Ajouter", ui =>
            {
                Vehicle vehicle = new Vehicle();
                vehicle.Name = "";
                vehicle.ModelId = 0;
                vehicle.Color = "#ffffff";
                vehicle.Serigraphie = "";

                BetterVehicleCreateOrUpdatePanel(player, vehicle);
            });
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPanel(player, AAMenu.AAMenu.menu.AdminTabLines);
            });
            panel.CloseButton();

            panel.Display();
        }
        public async void BetterVehicleCreateOrUpdatePanel(Player player, Vehicle vehicle, bool isDelete = false)
        {
            Panel panel = PanelHelper.Create("BetterVehicle - Ajouter/Modifier un véhicule", UIPanel.PanelType.TabPrice, player, () => BetterVehicleCreateOrUpdatePanel(player, vehicle));

            panel.AddTabLine($"{mk.Color("Nom:", mk.Colors.Info)} {vehicle.Name}", ui => SetVehicleName(player, vehicle));

           if(vehicle.Name != null && vehicle.Name.Length > 0)
            {
                panel.AddTabLine($"{mk.Color("Modèle:", mk.Colors.Info)} {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}", "", VehicleUtils.GetIconId(vehicle.ModelId), ui => SetVehicleModel(player, vehicle));

                panel.AddTabLine($"{mk.Color("Prix:", mk.Colors.Info)} {vehicle.Price} €", ui => SetVehiclePrice(player, vehicle));

                panel.AddTabLine($"{mk.Color("Couleur:", mk.Colors.Info)} {mk.Color(vehicle.Color, vehicle.Color)}", ui => SetVehicleColor(player, vehicle));

                bool urlIsValid = await InputUtils.IsValidImageLink(vehicle.Serigraphie);
                panel.AddTabLine($"{mk.Color("Sérigraphie:", mk.Colors.Info)} {(urlIsValid ? "valide" : "aucune")}", ui => SetVehicleSerigraphie(player, vehicle));
            }

            panel.NextButton("Sélectionner", () => panel.SelectTab());

            if (!isDelete) panel.NextButton("Supprimer", () => BetterVehicleCreateOrUpdatePanel(player, vehicle, true));
            else panel.PreviousButtonWithAction($"{mk.Size("Confirmer la<br>suppression", 12)}", async () =>
            {
                if (await vehicle.Delete())
                {
                    player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                    return true;
                }
                else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                return false;
            });

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        #region SETTERS
        public void SetVehicleName(Player player, Vehicle vehicle)
        {
            Panel panel = PanelHelper.Create("BetterVehicle - Modifier le nom", UIPanel.PanelType.Input, player, () => SetVehicleName(player, vehicle));

            panel.TextLines.Add("Donner un nom à ce véhicule");
            panel.inputPlaceholder = "3 caractères minimum";

            panel.PreviousButtonWithAction("Sélectionner", async () =>
            {
                if(panel.inputText.Length >= 3)
                {
                    vehicle.Name = panel.inputText;
                    if (await vehicle.Save())
                    {
                        player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                        return true;
                    }
                    else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                }
                else player.Notify("BetterVehicle", "3 caractères minimum", NotificationManager.Type.Warning);

                return false;
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

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
                        vehicle.Price = Math.Ceiling(price);
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

        public void SetVehicleColor(Player player, Vehicle vehicle)
        {
            Panel panel = PanelHelper.Create("BetterVehicle - Modifier la couleur", UIPanel.PanelType.Input, player, () => SetVehicleColor(player, vehicle));

            panel.TextLines.Add("Renseigner le code hexadecimal de la couleur de ce véhicule");
            panel.inputPlaceholder = "#ffffff";

            panel.PreviousButtonWithAction("Sélectionner", async () =>
            {
                if (panel.inputText.Length > 0)
                {
                    if (InputUtils.IsValidHexCode(panel.inputText))
                    {
                        vehicle.Color = panel.inputText;
                        if (await vehicle.Save())
                        {
                            player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                            return true;
                        }
                        else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                    }
                    else player.Notify("BetterVehicle", "Code hexadécimal invalide", NotificationManager.Type.Warning);
                }
                else player.Notify("BetterVehicle", "Renseigner une valeur", NotificationManager.Type.Warning);

                return false;
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public void SetVehicleSerigraphie(Player player, Vehicle vehicle)
        {
            Panel panel = PanelHelper.Create("BetterVehicle - Modifier la sérigraphie", UIPanel.PanelType.Input, player, () => SetVehicleSerigraphie(player, vehicle));

            panel.TextLines.Add("Renseigner l'URL du flocage");
            panel.inputPlaceholder = "";

            panel.PreviousButtonWithAction("Sélectionner", async () =>
            {
                if (await InputUtils.IsValidImageLink(panel.inputText))
                {
                    vehicle.Serigraphie = panel.inputText;
                    if (await vehicle.Save())
                    {
                        player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                        return true;
                    }
                    else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                }
                else player.Notify("BetterVehicle", "URL invalide", NotificationManager.Type.Warning);

                return false;
            });
            panel.PreviousButtonWithAction("Retirer", async () =>
            {
                vehicle.Serigraphie = null;
                if (await vehicle.Save())
                {
                    player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                    return true;
                }
                else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);

                return false;
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public void SetVehicleModel(Player player, Vehicle vehicle)
        {
            Panel panel = PanelHelper.Create("BetterVehicle - Modifier le modèle", UIPanel.PanelType.TabPrice, player, () => SetVehicleModel(player, vehicle));

            foreach (var (model, index) in Nova.v.vehicleModels.Select((model, index) => (model, index)))
            {
                if (!model.isDeprecated)
                {
                    panel.AddTabLine($"{model.vehicleName}", $"{(vehicle.ModelId == index ? $"{mk.Color("ACTUEL", mk.Colors.Success)}" : "")}", VehicleUtils.GetIconId(index), async _ =>
                    {
                        vehicle.ModelId = index;
                        if (await vehicle.Save())
                        {
                            player.Notify("BetterVehicle", "Modification enregistrée", NotificationManager.Type.Success);
                            panel.Previous();
                        }
                        else player.Notify("BetterVehicle", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                    });
                }
            }

            panel.AddButton("Sélectionner", _ => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion
    }
}
