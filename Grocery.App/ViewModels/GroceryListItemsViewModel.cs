using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;
        private readonly IGroceryListService _groceryListService;

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);
        [ObservableProperty]
        string myMessage;

        // alle beschikbare producten
        private IEnumerable<Product> _allAvailableProducts = [];

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService, IGroceryListService groceryListService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;
            _groceryListService = groceryListService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            _allAvailableProducts = _productService.GetAll()
                .Where(p => MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null && p.Stock > 0);
            foreach (var product in _allAvailableProducts)
            {
                AvailableProducts.Add(product);
            }
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);
            AvailableProducts.Remove(product);
            OnGroceryListChanged(GroceryList);
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                //await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
                
                await Shell.Current.DisplayAlert("Succes", $"Boodschappenlijst is opgeslagen:\n{_fileSaverService.FilePath}", "OK");
            }
            catch (Exception ex)
            {
                //await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
                await Shell.Current.DisplayAlert("Fout", $"Opslaan mislukt: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void SearchProducts(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // reset producten
                AvailableProducts.Clear();
                foreach (Product product in _allAvailableProducts)
                {
                    AvailableProducts.Add(product);
                }
                return;
            }

            // producten filteren
            var filteredProducts = _allAvailableProducts
                .Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            AvailableProducts.Clear();
            foreach (Product product in filteredProducts)
            {
                AvailableProducts.Add(product);
            }
            // geen producten gevonden
            if (AvailableProducts.Count == 0)
            {
                Shell.Current.DisplayAlert("Geen producten gevonden", "Probeer een andere zoekterm", "OK");
            }
        }

        [RelayCommand]
        private async Task RenameList()
        {
            // popup met invoerveld tonen
            string result = await Shell.Current.DisplayPromptAsync(
                "Lijst hernoemen",
                "Nieuwe naam voor de boodschappenlijst:",
                initialValue: GroceryList.Name,
                maxLength: 50,
                keyboard: Keyboard.Text);

            // naam wijzigen indien geldig
            if (!string.IsNullOrWhiteSpace(result) && result != GroceryList.Name)
            {
                GroceryList.Name = result;
                OnPropertyChanged(nameof(GroceryList));
                _groceryListService.Update(GroceryList);
                await Shell.Current.DisplayAlert("Succes", "De lijst is hernoemd.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Fout", "De naam is niet gewijzigd.", "OK");
            }
        }
    }
}
