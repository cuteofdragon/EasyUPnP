﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyUPnP
{
    public class MediaServer
    {
        #region Events

        public event EventHandler<ContentFoundEventArgs> OnContentFound;
        public class ContentFoundEventArgs : EventArgs
        {
            public ContentFoundEventArgs(Content content)
            {
                Content = content;
            }

            public Content Content { get; set; }
        }
        public event EventHandler<VideoContentScanCompletedEventArgs> OnVideoContentScanCompleted;
        public class VideoContentScanCompletedEventArgs : EventArgs
        {
            public VideoContentScanCompletedEventArgs(Content content)
            {
                Content = content;
            }

            public Content Content { get; set; }
        }
        public event EventHandler<AudioContentScanCompletedEventArgs> OnAudioContentScanCompleted;
        public class AudioContentScanCompletedEventArgs : EventArgs
        {
            public AudioContentScanCompletedEventArgs(Content content)
            {
                Content = content;
            }

            public Content Content { get; set; }
        }
        public event EventHandler<PhotoContentScanCompletedEventArgs> OnPhotoContentScanCompleted;
        public class PhotoContentScanCompletedEventArgs : EventArgs
        {
            public PhotoContentScanCompletedEventArgs(Content content)
            {
                Content = content;
            }

            public Content Content { get; set; }
        }

        #endregion
        
        public MediaServer()
        {
        }

        public MediaServer(DeviceDescription deviceDescription, Uri aliasUrl, Uri deviceDescriptionUrl, bool is_online_media_server)
        {
            if (deviceDescription == null)
            {
                this.AliasURL = aliasUrl.AbsoluteUri;
                this.FriendlyName = "NOT SUPPORTED";
                this.ConnectionUrl = "Resources/online_server.png";
                this.IconUrl = "Resources/not_supported.png";
                this.OnlineServer = is_online_media_server;
                return;
            }

            this.DeviceDescription = deviceDescription;
            this.FriendlyName = this.DeviceDescription.Device.FriendlyName;
            this.UDN = this.DeviceDescription.Device.UDN;
            this.AliasURL = aliasUrl.AbsoluteUri;
            this.OnlineServer = is_online_media_server;

            if (this.OnlineServer)
            {
                this.PresentationURL = deviceDescriptionUrl.Authority;
                if (this.PresentationURL.IndexOf("http://") == -1)
                    this.PresentationURL = "http://" + this.PresentationURL;
                this.PresentationURL = this.PresentationURL + UPnPService.UseSlash(this.PresentationURL);
                this.ConnectionUrl = "Resources/online_server.png";
            }
            else
            {
                if (string.IsNullOrEmpty(this.DeviceDescription.Device.PresentationURL))
                {
                    this.DeviceDescription.Device.PresentationURL = deviceDescriptionUrl.Authority;
                    if (this.DeviceDescription.Device.PresentationURL.IndexOf("http://") == -1)
                        this.DeviceDescription.Device.PresentationURL = "http://" + this.DeviceDescription.Device.PresentationURL;
                }
                this.PresentationURL = this.DeviceDescription.Device.PresentationURL + UPnPService.UseSlash(this.DeviceDescription.Device.PresentationURL);
                this.ConnectionUrl = "Resources/intranet_server.png";
            }
        }

        public string UDN { get; private set; }
        public string AliasURL { get; private set; }
        public string FriendlyName { get; private set; }
        public string PresentationURL { get; private set; }
        public string IconUrl { get; private set; }
        public bool OnlineServer { get; private set; }
        public string ConnectionUrl { get; private set; }
        public MediaServer Self { get; private set; }

        public DeviceDescription DeviceDescription { get; private set; }
        public ConnectionManager ConnectionManager { get; private set; }
        public ContentDirectory ContentDirectory { get; private set; }
        public MediaReceiverRegistrar MediaReceiverRegistrar { get; private set; }
        public Uri ContentDirectoryControlUrl { get; private set; }
        
        private Action _browseAction;
        private Dictionary<string, Content> _contents = new Dictionary<string,Content>();

        #region Public functions

        public async Task InitAsync()
        {
            if (this.DeviceDescription == null)
                return;

            foreach (Service serv in this.DeviceDescription.Device.ServiceList)
                switch (serv.ServiceType)
                {
                    case ServiceTypes.CONNECTIONMANAGER:
                        this.ConnectionManager = await Deserializer.DeserializeXmlAsync<ConnectionManager>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
                        break;
                    case ServiceTypes.CONTENTDIRECTORY:
                        this.ContentDirectory = await Deserializer.DeserializeXmlAsync<ContentDirectory>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
                        this.ContentDirectoryControlUrl = new Uri(this.PresentationURL + serv.ControlURL.Substring(1));
                        break;
                    case ServiceTypes.MEDIARECEIVERREGISTRAR:
                        this.MediaReceiverRegistrar = await Deserializer.DeserializeXmlAsync<MediaReceiverRegistrar>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
                        break;
                }

            SetIconUrl();
            _browseAction = FindBrowseAction();
            this.Self = this;
        }

        public async Task BrowseFolderAsync(string destination_id, string go_up_id)
        {
            try
            {
                Content content;
                if (!_contents.ContainsKey(destination_id))
                {
                    content = new Content(await BrowseFolderAsync(destination_id), go_up_id, this.PresentationURL);
                    _contents.Add(destination_id, content);
                }
                else
                    content = _contents[destination_id];


                if (OnContentFound != null)
                    OnContentFound(this, new ContentFoundEventArgs(content));
            }
            catch
            {
            }
        }

        public void GoUp(Content content)
        {
            if (_contents.ContainsKey(content.GoUpId))
            {
                if (OnContentFound != null)
                    OnContentFound(this, new ContentFoundEventArgs(_contents[content.GoUpId]));
            }
        }

        public async Task StartScanVideoContentAsync()
        {
            try
            {
                //root
                DIDLLite didllite_root = await BrowseFolderAsync("0");
                foreach (Container cont_root in didllite_root.Containers)
                {
                    if (cont_root.PersistentID == "video")
                    {
                        DIDLLite didllite_video = await BrowseFolderAsync(cont_root.Id);
                        foreach (Container cont_video in didllite_video.Containers)
                        {
                            if (cont_video.PersistentID == "video/all")
                            {
                                DIDLLite didllite_allvideo = await BrowseFolderAsync(cont_video.Id);
                                Content content = new Content(didllite_allvideo, cont_video.ParentID, this.PresentationURL);

                                if (OnVideoContentScanCompleted != null)
                                    OnVideoContentScanCompleted(this, new VideoContentScanCompletedEventArgs(content));
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public async Task StartScanAudioContentAsync()
        {
            try
            {
                //root
                DIDLLite didllite_root = await BrowseFolderAsync("0");
                foreach (Container cont_root in didllite_root.Containers)
                {
                    if (cont_root.PersistentID == "music")
                    {
                        DIDLLite didllite_audio = await BrowseFolderAsync(cont_root.Id);
                        foreach (Container cont_audio in didllite_audio.Containers)
                        {
                            if (cont_audio.PersistentID == "music/all")
                            {
                                DIDLLite didllite_allaudio = await BrowseFolderAsync(cont_audio.Id);
                                Content content = new Content(didllite_allaudio, cont_audio.ParentID, this.PresentationURL);

                                if (OnAudioContentScanCompleted != null)
                                    OnAudioContentScanCompleted(this, new AudioContentScanCompletedEventArgs(content));
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public async Task StartScanPhotoContentAsync()
        {
            try
            {
                //root
                DIDLLite didllite_root = await BrowseFolderAsync("0");
                foreach (Container cont_root in didllite_root.Containers)
                {
                    if (cont_root.PersistentID == "picture")
                    {
                        DIDLLite didllite_photo = await BrowseFolderAsync(cont_root.Id);
                        foreach (Container cont_photo in didllite_photo.Containers)
                        {
                            if (cont_photo.PersistentID == "picture/all")
                            {
                                DIDLLite didllite_allphoto = await BrowseFolderAsync(cont_photo.Id);
                                Content content = new Content(didllite_allphoto, cont_photo.ParentID, this.PresentationURL);

                                if (OnPhotoContentScanCompleted != null)
                                    OnPhotoContentScanCompleted(this, new PhotoContentScanCompletedEventArgs(content));
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Private functions
        
        private void SetIconUrl()
        {
            try
            {
                Icon[] iconList = DeviceDescription.Device.IconList;
                if (iconList != null)
                {
                    foreach (Icon ic in iconList)
                    {
                        if (ic.MimeType.IndexOf("png") > -1)
                        {
                            this.IconUrl = ic.Url;
                        }
                    }
                    if (string.IsNullOrEmpty(this.IconUrl) && (iconList != null))
                        this.IconUrl = iconList[0].Url;

                    if (!string.IsNullOrEmpty(this.IconUrl))
                    {
                        if (this.IconUrl.Substring(0, 1) == "/")
                            this.IconUrl = this.IconUrl.Substring(1);
                        this.IconUrl = this.PresentationURL + this.IconUrl;
                    }
                }
            }
            catch
            {
            }
        }

        private Action FindBrowseAction()
        {
            if (this.ContentDirectory != null)
            {
                foreach (Action act in this.ContentDirectory.ActionList)
                {
                    if (act.Name.ToUpper() == "BROWSE")
                    {
                        return act;
                    }
                }
            }

            return null;
        }

        private async Task<DIDLLite> BrowseFolderAsync(string id)
        {
            try
            {
                if (_browseAction != null)
                {
                    DIDLLite didllite = new DIDLLite();
                    didllite.Containers = new List<Container>();
                    didllite.Items = new List<Item>();
                    int start_from = 0;
                    int limit = 100;
                    bool found = false;
                    do
                    {
                        _browseAction.ClearArgumentsValue();
                        _browseAction.SetArgumentValue("ObjectId", id);
                        _browseAction.SetArgumentValue("BrowseFlag", "BrowseDirectChildren");
                        _browseAction.SetArgumentValue("Filter", "*");
                        _browseAction.SetArgumentValue("StartingIndex", start_from.ToString());
                        _browseAction.SetArgumentValue("RequestedCount", limit.ToString());
                        _browseAction.SetArgumentValue("SortCriteria", "");
                        await _browseAction.InvokeAsync(ServiceTypes.CONTENTDIRECTORY, this.ContentDirectoryControlUrl.AbsoluteUri);
                        DIDLLite tmp_didllite = Deserializer.DeserializeXml<DIDLLite>(_browseAction.GetArgumentValue("Result"));
                        foreach (Container container in tmp_didllite.Containers)
                            didllite.Containers.Add(container);
                        foreach (Item item in tmp_didllite.Items)
                            didllite.Items.Add(item);
                        found = (tmp_didllite.Containers.Count > 0 || tmp_didllite.Items.Count > 0);
                        start_from += limit;
                    }
                    while (found);

                    return didllite;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
