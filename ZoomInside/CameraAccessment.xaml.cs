﻿using Camera.MAUI;
using RestSharp;
using System.Reflection.Emit;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Firebase.Database;
using Mopups.Services;
using Mopups.Interfaces;
//using Mopups.Services;
//using Mopups.Interfaces;
//using MauiMopupsSample;

namespace ZoomInside;

public partial class CameraAccessment : ContentPage
{
    FirebaseClient firebaseClient =
            new FirebaseClient("https://zoominside-2ccf3-default-rtdb.europe-west1.firebasedatabase.app/");
    private async Task LoadDataAsync()
    {
        // Simulate loading data asynchronously
        await Task.Delay(3000); // Placeholder for actual data loading code
    }
    public double ScreenWidth { get; set; }


    IPopupNavigation popupNavigation;
    public CameraAccessment(IPopupNavigation popupNavigation)
	{
		InitializeComponent();

        // Get the device's screen width
        ScreenWidth = Math.Round(DeviceDisplay.MainDisplayInfo.Width / 3);

        // Set the binding context to the current instance of the page (this)
        this.BindingContext = this;

        this.popupNavigation = popupNavigation;
    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        apiManipulation apiManip = new apiManipulation();

        byte[] imageBytes = null;
        try
        {
            // Using MediaPicker to interact with the device camera
            var photo = await MediaPicker.CapturePhotoAsync();

            if (photo != null)
            {
                // Save the photo to a specific directory
                var fullPath = apiManip.CreateDirectory();

                var stream = await photo.OpenReadAsync();
                if (stream != null)
                {
                    using (MemoryStream toBytes = new MemoryStream())
                    {
                        await stream.CopyToAsync(toBytes);
                        imageBytes = toBytes.ToArray();
                    }
                }
                //testImg.Source = fullPath;
                await File.WriteAllBytesAsync(fullPath, imageBytes);

                //await DisplayAlert("Success", "Photo saved to: " + fullPath, "OK");
            }
        }
        catch (FeatureNotSupportedException fnsEx)
        {
            // Feature not supported on the device
            await DisplayAlert("Error", fnsEx.Message, "OK");
        }
        catch (PermissionException pEx)
        {
            // Permissions not granted
            await DisplayAlert("Error", pEx.Message, "OK");
        }
        catch (Exception ex)
        {
            // Other errors
            await DisplayAlert("Error", ex.Message, "OK");
        }

        // Showing the activity indicator
        activityIndicator.IsRunning = true;
        activityIndicator.IsVisible = true;
        await LoadDataAsync();


        // Accessing the API
        var extractedText = apiManip.TextExtract(imageBytes);

        // Removing the unnecessary characters from our text 
        var formattedText = apiManip.Format(extractedText);
        // Removing the escape symbols Visual Studio adds automatically to the unicode we received from the api
        var unescapedFormattedText = Regex.Unescape(formattedText);

        // Hide the activity indicator
        activityIndicator.IsRunning = false;
        activityIndicator.IsVisible = false;

        char[] splitCharacters = { '{', '}', ' ', ',', '[', ']' };
        List<string> resultTxt = unescapedFormattedText
            .Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        for (var i = 0; i < resultTxt.Count; i++)
        {
            resultTxt[i] = resultTxt[i].ToLower();
        }

        var firebaseObject = await firebaseClient.Child("Es").OnceAsync<EsItem>();
        List<EsItem> dataList = firebaseObject.Select(x => x.Object).ToList();

        List<string> propertyValues = new List<string>();
        foreach (var item in dataList)
        {
            propertyValues.Add(item.Info);
        }
        for (int i = 0; i < propertyValues.Count; i++)
        {
            propertyValues[i] = propertyValues[i].ToLower();
        }


        // propertyValues -> данните от файърбейз 
        // resultTxt -> данните от снимката 

        List<string> final = new List<string>();
        for (int i = 0; i < propertyValues.Count; i++)
        {
            int index = Math.Abs(propertyValues[i].IndexOf(":"));
            string auxiliaryVar = propertyValues[i].Substring(0, index);

            foreach (var item in resultTxt)
            {
                if (item == auxiliaryVar)
                {
                    final.Add(propertyValues[i]);
                    break;
                }
            }
        }

        await popupNavigation.PushAsync(new MyMopup(final));
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new AdminAuthentication());
    }
}