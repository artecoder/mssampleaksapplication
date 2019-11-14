using Windows.Devices.Enumeration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.SpeechRecognition;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Globalization;
using System.Text;
using Windows.UI.Core;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.ApplicationModel;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using System.Threading.Tasks;
using Windows.UI.Popups;
using IBM.WatsonDeveloperCloud.SpeechToText.v1;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SpeechApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SpeechRecognizer speechRecognizer;
        private bool recording;
        Stopwatch timer = new Stopwatch();
        MediaCapture mediaCapture = new MediaCapture();
        
        public MainPage()
        {
            this.InitializeComponent();
            this.recording = false;
            Windows.Storage.StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            listFiles(storageFolder.Path);
            //fechaEntrevista.Date = DateTime.Now;
            listMicrophones();
        }

        private async void listMicrophones()
        {
            var allAudioCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var item in allAudioCaptureDevices)
            {
                comboMicrofono.Items.Add(item.Name);
            }
            
        }

        private async void timerCallback(object state)
        {
            // do some work not connected with UI

            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    // do some work on UI here;
                });
        }
        private async void listFiles(string folder)
        {
            StorageFolder appInstalledFolder = Package.Current.InstalledLocation;
            IReadOnlyList<StorageFile> files = await appInstalledFolder.GetFilesAsync();
            Debug.WriteLine("File Count: " + files.Count);
            listAudios.Items.Clear();
            foreach (StorageFile file in files)
            {
                if (file.Name.Contains(".wav"))
                {
                    try
                    {
                        RelativePanel RelativePanelItems = new RelativePanel();
                        RelativePanelItems.Width = 700;

                        Button newButton = new Button();
                        newButton.Name = file.Name.Replace(".", "_");
                        newButton.Content = file.Name;
                        newButton.Click += Button_Click;
                        newButton.HorizontalAlignment = HorizontalAlignment.Left;
                        

                        //Botón de borrar
                        Button deleteButton = new Button();
                        StackPanel deleteButtonImageStackPanel = new StackPanel();
                        SymbolIcon deleteSymbolIcon = new SymbolIcon();
                        deleteSymbolIcon.Symbol = Symbol.Delete;
                        deleteButtonImageStackPanel.Children.Add(deleteSymbolIcon);
                        RelativePanel.SetAlignRightWithPanel(deleteButton,true);
                        ToolTipService.SetToolTip(deleteButton, "Eliminar audio y transcripción");
                        deleteButton.HorizontalAlignment = HorizontalAlignment.Right;
                        deleteButton.Content = deleteButtonImageStackPanel;

                        //Botón de renombrar
                        Button renameButton = new Button();
                        StackPanel renameButtonImageStackPanel = new StackPanel();
                        SymbolIcon renameSymbolIcon = new SymbolIcon();
                        renameSymbolIcon.Symbol = Symbol.Comment;
                        renameButtonImageStackPanel.Children.Add(renameSymbolIcon);
                        RelativePanel.SetLeftOf(renameButton, deleteButton);
                        ToolTipService.SetToolTip(renameButton, "Cambiar nombre de la entrevista");
                        renameButton.Content = renameButtonImageStackPanel;

                        //Botón de nueva transcripción
                        Button sttButton = new Button();
                        StackPanel sttButtonImageStackPanel = new StackPanel();
                        SymbolIcon sttSymbolIcon = new SymbolIcon();
                        sttSymbolIcon.Symbol = Symbol.Globe;
                        sttButton.Name= "stt_"+file.Name.Replace(".", "_");
                        sttButtonImageStackPanel.Children.Add(sttSymbolIcon);
                        RelativePanel.SetLeftOf(sttButton, renameButton);
                        ToolTipService.SetToolTip(sttButton, "Iniciar nueva transcripción de audio a texto");
                        sttButton.Content = sttButtonImageStackPanel;
                        sttButton.Click += SttButton_Click;

                        RelativePanelItems.Children.Add(newButton);
                        RelativePanelItems.Children.Add(deleteButton);
                        RelativePanelItems.Children.Add(renameButton);
                        RelativePanelItems.Children.Add(sttButton);


                        listAudios.Items.Add(RelativePanelItems);

                    }
                    catch 
                    { 
                    }
                }
                Debug.WriteLine(file.Name);

            }
        }

        private void SttButton_Click(object sender, RoutedEventArgs e)
        {
            //        IamAuthenticator authenticator = new IamAuthenticator(
            //apikey: "{apikey}"
            //);
            IBM.WatsonDeveloperCloud.Util.TokenOptions op = new IBM.WatsonDeveloperCloud.Util.TokenOptions();
            op.IamApiKey = "eSsYRW5RJIqXA2chLqxgBjC7ZBiYYa3k78QUm8Qg8Y1W";
            SpeechToTextService sttService = new SpeechToTextService(op);
            sttService.SetEndpoint("https://stream.watsonplatform.net/speech-to-text/api");
            var ResultModel=sttService.GetModel(
                modelId: "es-ES_BroadbandModel");
            StorageFolder appInstalledFolder = Package.Current.InstalledLocation;

            //var Result= sttService.Recognize(audio:
            //Debug.WriteLine(Result.ResponseJson);

        }

        private async Task<Boolean> CaptureAsync() 
        {
            bool ok = true;
            try
            {
                bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission(comboMicrofono.SelectedIndex);
                //Language newLanguage = new Language("es");
                this.speechRecognizer = new SpeechRecognizer(SpeechRecognizer.SystemSpeechLanguage);
                var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
                speechRecognizer.Constraints.Add(dictationConstraint);
                SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
                if (speechRecognizer.State == SpeechRecognizerState.Idle)
                {
                    try
                    {
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch { }
                }
                speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            }
            catch
            {
                ok = false;
            }
            return ok;
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            bool okCapture = false;
            bool okTitle = false;
            try
            {
                if (!recording)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        DictationButtonText.Text = "Detener";
                        dictationSymbolIcon.Symbol = Symbol.Stop;
                        recording = true;
                    });
                    okCapture = await CaptureAsync();
                    okTitle = await validateTitle();
                    
                }
                string title = getTitle();
                if (!recording || !okCapture || !okTitle)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        DictationButtonText.Text = "Iniciar";
                        dictationSymbolIcon.Symbol = Symbol.Microphone;
                    });
                    recording = false;
                    try
                    {
                        this.speechRecognizer.Dispose();
                        this.speechRecognizer = null;
                    }
                    catch { }
                    timer.Stop();
                    await mediaCapture.StopRecordAsync();
                    Windows.Storage.StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                    listFiles(storageFolder.Path);   
                    saveText(title);
                }
                else
                {
                    RecordAsync(title);
                    timer.Start();
                }
            }
            catch
            {
                
            }
        }

        private async void saveText(string title)
        {

            StorageFolder newFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Windows.Storage.StorageFile file = await newFolder.CreateFileAsync(title + ".rtf", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            if (file != null)
            {
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                Windows.Storage.Streams.IRandomAccessStream randAccStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox = new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }

        private string getTitle()
        {
            return txtNombreEntrevista.Text;
        }

        private async Task<bool> validateTitle()
        {
            bool retorno = true;
            if (txtNombreEntrevista.Text.Equals(""))
            {
                var dialog = new MessageDialog("Debe indicar un nombre de la entrevista a realizar");
                await dialog.ShowAsync();
                retorno = false;
            }
            if (txtNombreEntrevista.Text.Contains(@"\") || 
                txtNombreEntrevista.Text.Contains("/") ||
                txtNombreEntrevista.Text.Contains(":") ||
                txtNombreEntrevista.Text.Contains("*") ||
                txtNombreEntrevista.Text.Contains("?") ||
                txtNombreEntrevista.Text.Contains("\"") ||
                txtNombreEntrevista.Text.Contains("<") ||
                txtNombreEntrevista.Text.Contains(">") ||
                txtNombreEntrevista.Text.Contains("|"))
            {
                var dialog = new MessageDialog("Debe indicar un nombre sin caracteres especiales, Se debe generar un archivo, los nombres de archivo no pueden contener ninguno de los siguientes caracteres  '\\, /, :, *, ?, \", <, >, |' ");
                await dialog.ShowAsync();
                retorno = false;
            }
            if (txtNombreEntrevista.Text.Contains("_"))
            {
                var dialog = new MessageDialog("El caracter '_' está reservado para uso exclusivo de la aplicación. Especifique un título sin este caracter");
                await dialog.ShowAsync();
                retorno = false;
            }
            return retorno;
        }

        private async void RecordAsync(string title)
        {
            mediaCapture = new MediaCapture();

            Windows.Storage.StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            #region Permisos al folder
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            //StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            //if (folder != null)
            //{
            //    StorageApplicationPermissions.FutureAccessList.AddOrReplace(@"Assets\Audios", folder);
            //}
            StorageFolder newFolder= Windows.ApplicationModel.Package.Current.InstalledLocation;
            //await newFolder.CreateFileAsync("test.txt");
            #endregion
            string fileName = title + ".wav";
            Windows.Storage.StorageFile sampleFile = null;
            sampleFile = await newFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            
            var mediaEncodingProfile = Windows.Media.MediaProperties.MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
            var captureSettings = new MediaCaptureInitializationSettings();
            captureSettings.StreamingCaptureMode = StreamingCaptureMode.Audio;
            await mediaCapture.InitializeAsync(captureSettings);

            await mediaCapture.StartRecordToStorageFileAsync(mediaEncodingProfile, sampleFile);

            
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string interlocutor = string.Empty;
                if (radioInterlocutor1.IsChecked.Value)
                {
                    interlocutor = txtInterlocutor1.Text;
                }
                else if (radioInterlocutor2.IsChecked.Value)
                {
                    interlocutor = txtInterlocutor2.Text;
                }
                else if (radioInterlocutor3.IsChecked.Value)
                {
                    interlocutor = txtInterlocutor3.Text;
                }
                else if (radioInterlocutor4.IsChecked.Value)
                {
                    interlocutor = txtInterlocutor4.Text;
                }
                else if (radioInterlocutor5.IsChecked.Value)
                {
                    interlocutor = txtInterlocutor5.Text;
                }

                string value = string.Empty;
                editor.Document.GetText(Windows.UI.Text.TextGetOptions.AdjustCrlf, out value);
                string timerTime = "(" + timer.Elapsed.ToString().Split('.')[0] + ")";
                editor.Document.SetText(Windows.UI.Text.TextSetOptions.None, value + "\n" + interlocutor+ timerTime+": "+args.Result.Text);
                editor.Document.GetText(Windows.UI.Text.TextGetOptions.AdjustCrlf, out value);
                var newPos = value.Length;
                editor.Document.Selection.SetRange(newPos, newPos);
                editor.Focus(FocusState.Keyboard);
            });
        }
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // Open a text file.
            Windows.Storage.Pickers.FileOpenPicker open =
                new Windows.Storage.Pickers.FileOpenPicker();
            open.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".trf");

            Windows.Storage.StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    Windows.Storage.Streams.IRandomAccessStream randAccStream =
                await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    // Load the file into the Document property of the RichEditBox.
                    editor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                }
                catch (Exception)
                {
                    ContentDialog errorDialog = new ContentDialog()
                    {
                        Title = "File open error",
                        Content = "Sorry, I couldn't open the file.",
                        PrimaryButtonText = "Ok"
                    };

                    await errorDialog.ShowAsync();
                }
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileSavePicker savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Text Rich Format", new List<string>() { ".trf" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "Transcripción de entrevista";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                // write to file
                Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

                editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }
        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Bold = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }
        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Italic = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }
        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                if (charFormatting.Underline == Windows.UI.Text.UnderlineType.None)
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.Single;
                }
                else
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.None;
                }
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (recording)
            {
                //Continua grabando
                await CaptureAsync();
            }
            else
            {
                try
                {
                    this.speechRecognizer.Dispose();
                    this.speechRecognizer = null;
                }
                catch { }
            }
        }

        private void dictatedTextBuilder_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void radioInterlocutor2_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Pausa_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder newFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Windows.Storage.StorageFile file = await newFolder.GetFileAsync(((Button)(sender)).Content.ToString());
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            string fileName = ((Button)(sender)).Name;
            await mediaElement1.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                mediaElement1.SetSource(stream, file.ContentType);
                mediaElement1.Play();
            });
            LoadText(fileName);
            if (txtNombreEntrevista.Text.Equals(""))
            {
                txtNombreEntrevista.Text = fileName.Split('_')[0];
            }
            //foregroundMediaControl.
        }

        private async void LoadText(string fileName)
        {
            StorageFolder newFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            try
            {
                Stream randAccStream = await newFolder.OpenStreamForReadAsync(fileName.Split('_')[0] + ".rtf");
                // Load the file into the Document property of the RichEditBox.
                editor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream.AsRandomAccessStream());
            }
            catch (Exception)
            {
                ContentDialog errorDialog = new ContentDialog()
                {
                    Title = "File open error",
                    Content = "Sorry, I couldn't open the file.",
                    PrimaryButtonText = "Ok"
                };

                await errorDialog.ShowAsync();
            }
        }
    }
}
