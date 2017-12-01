using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Render;
using Windows.Storage;

namespace WindowsIotAudio.Models
{

    public class PlayFileMessage
    {
        public string FilePath { get; }

        public PlayFileMessage(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class PlayMediaSourceMessage
    {
        public MediaSource MediaSource { get; }

        public PlayMediaSourceMessage(MediaSource mediaSource)
        {
            MediaSource = mediaSource;
        }
    }

    public class PlaySoundMessage
    {
        public StorageFile AudioFile { get; }

        public PlaySoundMessage(StorageFile audioFile)
        {
            AudioFile = audioFile;
        }
    }

    public class AudioEngine
    {
        static AudioEngine instance;

        public Dictionary<string, AudioGraph> AudioGraphs { get; }
        private AudioFileInputNode fileInputNode1, fileInputNode2;

        public static AudioEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AudioEngine();
                }
                return instance;
            }
        }

        AudioEngine()
        {
            AudioGraphs = new Dictionary<string, AudioGraph>();
        }

        public async Task<AudioGraph> CreateAudioGraph(string filePath)
        {
            AudioGraph audioGraph;
            AudioDeviceOutputNode deviceOutputNode;

            // Create an AudioGraph with default setting
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.GameEffects);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                Debug.WriteLine("Could not create an audio graph");
                return null;
            }

            audioGraph = result.Graph;

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await audioGraph.CreateDeviceOutputNodeAsync();

            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.WriteLine("Could not create an output node");
                return null;
            }

            deviceOutputNode = deviceOutputNodeResult.DeviceOutputNode;

            AudioFileInputNode fileInput;

            // @"Audio\correctAnswerPlayer2.wav"
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(filePath);
            if (file == null) return null;

            CreateAudioFileInputNodeResult fileInputResult = await audioGraph.CreateFileInputNodeAsync(file);
            if (AudioFileNodeCreationStatus.Success != fileInputResult.Status)
            {
                // Cannot read input file
                Debug.WriteLine($"Cannot read input file because {fileInputResult.Status.ToString()}");
                return null;
            }

            fileInput = fileInputResult.FileInputNode;

            fileInput.AddOutgoingConnection(deviceOutputNode);

            return audioGraph;
        }

        public async Task<AudioGraph> RegisterAudio(string name, string filePath)
        {
            try
            {
                if (AudioGraphs.ContainsKey(name))
                {
                    Debug.WriteLine($"AudioEngine: {name} is already registered");
                    return null;
                }
                var audioGraph = await CreateAudioGraph(filePath);
                AudioGraphs.Add(name, audioGraph);
                Debug.WriteLine($"AudioEngine: Registered {name}");
                return audioGraph;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AudioEngine: Failed to register {name}");
                return null;
            }
        }
    }
}
