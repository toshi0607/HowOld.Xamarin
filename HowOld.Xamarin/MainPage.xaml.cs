﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Plugin.Connectivity;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace HowOld.Xamarin
{
    public partial class MainPage : ContentPage
    {
        private readonly IFaceServiceClient _faceServiceClient;

        public MainPage()
        {
            InitializeComponent();
            _faceServiceClient = new FaceServiceClient(Variables.FACE_API_KEY, "https://eastasia.api.cognitive.microsoft.com/face/v1.0");
        }

        private async void UploadPictureButton_Clicked(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await DisplayAlert("アップロードできません", "この端末は写真の選択をサポートしていません", "OK");
                return;
            }
            var file = await CrossMedia.Current.PickPhotoAsync();
            if (file == null)
                return;
            Indicator1.IsRunning = true;
            Image1.Source = ImageSource.FromStream(() => file.GetStream());

            FaceDetection theData = await DetectFaceAsync(file);
            BindingContext = theData;
            Indicator1.IsRunning = false;
        }

        private async void TakePictureButton_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.
              IsTakePhotoSupported)
            {
                await DisplayAlert("カメラが使用できません", "この端末はカメラの使用をサポートしていません", "OK");
                return;
            }
            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                SaveToAlbum = true,
                Name = "test.jpg"
            });
            if (file == null)
                return;
            Indicator1.IsRunning = true;
            Image1.Source = ImageSource.FromStream(() => file.GetStream());

            FaceDetection theData = await DetectFaceAsync(file);
            BindingContext = theData;
            Indicator1.IsRunning = false;
        }

        private async Task<FaceDetection> DetectFaceAsync(MediaFile inputFile)
        {

            if (!CrossConnectivity.Current.IsConnected)
            {
                await DisplayAlert("ネットワークエラー", "ネット接続を確認してからリトライしてください。", "OK");
                return null;
            }

            try
            {
                var faces = await _faceServiceClient.DetectAsync(inputFile.GetStream(), false, false, (FaceAttributeType[])Enum.GetValues(typeof(FaceAttributeType)));

                if (faces.Length == 0) {
                    throw new Exception("顔を認識できませんでした。別の写真を試してください。");
                }

                var faceAttributes = faces[0]?.FaceAttributes;
                var faceDetection = new FaceDetection();
                faceDetection.Age = faceAttributes.Age;
                faceDetection.Emotion = faceAttributes.Emotion.ToRankedList().FirstOrDefault().Key;
                faceDetection.Glasses = faceAttributes.Glasses.ToString();
                faceDetection.Smile = faceAttributes.Smile;
                faceDetection.Gender = faceAttributes.Gender;
                faceDetection.Moustache = faceAttributes.FacialHair.Moustache;
                faceDetection.Beard = faceAttributes.FacialHair.Beard;

                return faceDetection;
            }
            catch(Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
        }
    }
}
