﻿using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Input;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.TeamFoundation.MVVM;
using RemoteAppManager;
using RemoteAppManager.Core;
using RemoteAppManager.Packets;
using RemoteAppManagerClient.Prototype;
using System.Threading;

namespace RemoteAppManagerClient.ViewModel
{
    class ClientViewModel : ClientViewModelBase
    {
        private ClientConnectionService _connection;
        private ConnectionStatuses _connectionStatus;
        private IPAddressPrototype _addressPrototype;
        private ProcessPrototypeCollection _processCollection;
        private ProcessPrototypeCollection _processToStartCollection;
        private ProcessPrototype _selectedPrototype;
        private ProcessPrototype _selectedProcessToStart;
        private String _processToStart;
        private String _processImageString;
        private ImageSource _processImage;
        private Boolean _isImageTransfering;
        private System.Timers.Timer _transferingTimer;

        #region Properties
        public ProcessPrototype SelectedPrototype {
            get { return _selectedPrototype; }
            set {
                _selectedPrototype = value;
                NotifyPropertyChanged("SelectedPrototype");
            }
        }

        public ConnectionStatuses ConnectionStatus {
            get { return _connectionStatus; }
            set {
                _connectionStatus = value;
                NotifyPropertyChanged("ConnectionStatus");
                NotifyPropertyChanged("IsReady");
                NotifyPropertyChanged("IsConnected");
            }
        }

        public ProcessPrototype SelectedProcessToStart
        {
            get { return _selectedProcessToStart; }
            set
            {
                _selectedProcessToStart = value;
                NotifyPropertyChanged("SelectedProcessToStart");
            }
        }

        public Boolean IsImageTransfering
        {
            get { return _isImageTransfering; }
            set
            {
                _isImageTransfering = value;

                if (value == false)
                    _transferingTimer.Stop();

                NotifyPropertyChanged("IsImageTransfering");
            }
        }
        #endregion

        #region View properties
        public ClientConnectionService Connection {
            get { return _connection; }
        }

        public IPAddressPrototype IPAddress {
            get {
                if (_addressPrototype == null) {
                    _addressPrototype = new IPAddressPrototype();
                }

                return _addressPrototype;
            }
        }

        public ProcessPrototypeCollection ProcessCollection {
            get {
                if (_processCollection == null) {
                    _processCollection = new ProcessPrototypeCollection();
                }

                return _processCollection;
            }
        }

        public ProcessPrototypeCollection ProcessToStartCollection {
            get {
                if (_processToStartCollection == null) {
                    _processToStartCollection = new ProcessPrototypeCollection();
                }

                return _processToStartCollection;
            }
        }

        public bool IsReady {
            get {
                return (Connection.Socket != null && !Connection.Socket.Connected && this.ConnectionStatus == ConnectionStatuses.DISCONNECTED);
            }
        }

        public bool IsConnected {
            get {
                return (Connection.Socket != null && Connection.Socket.Connected && this.ConnectionStatus == ConnectionStatuses.CONNECTED);
            }
        }

        public string ProcessToStart {
            get { return _processToStart; }
            set {
                _processToStart = value;
                NotifyPropertyChanged("ProcessToStart");
            }
        }

        public ImageSource ProcessImage
        {
            get { return _processImage; }
            set
            {
                _processImage = value;
                NotifyPropertyChanged("ProcessImage");
            }
        }

        #endregion

        #region Class
        public ClientViewModel() {
            _connection = new ClientConnectionService();
            _connection.ConnectionStateChanged += new ConnectionStateChangedEventHandler(_connection_ConnectionStateChangedEventHandler);
            _connection.MessageReceived += new MessageReceivedEventHandler(_connection_MessageReceivedEventHandler);
            _processImageString = "";
            _isImageTransfering = false;
            _transferingTimer = new System.Timers.Timer();
            _transferingTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            _transferingTimer.Interval = 5000;
            _transferingTimer.Enabled = true;

            CreateConnectCommand();
            CreateDisconnectCommand();
            CreateRequestProcessesCommand();
            CreateKillProcessCommand();
            CreateFindProcessCommand();
            CreateStartProcessCommand();
            CreateOpenImageCommand();
        }
        #endregion

        #region Commands
        private void CreateConnectCommand() {
            ConnectCommand = new RelayCommand(ConnectCommandExecute, CanExecuteConnectCommand);
        }

        public ICommand ConnectCommand {
            get;
            internal set;
        }

        private bool CanExecuteConnectCommand(Object param) {
            return !IsConnected;
        }

        private void ConnectCommandExecute(Object param) {
            IPAddress ipAddress = null;
            System.Net.IPAddress.TryParse(this.IPAddress.GetIPAdress(), out ipAddress);

            if (ipAddress != null) {
                Connection.Start(ipAddress);
            }
        }

        private void CreateDisconnectCommand() {
            DisconnectCommand = new RelayCommand(DisconnectCommandExecute, CanExecuteDisconnectCommand);
        }

        public ICommand DisconnectCommand {
            get;
            internal set;
        }

        private bool CanExecuteDisconnectCommand(Object param) {
            return IsConnected;
        }

        private void DisconnectCommandExecute(Object param) {
            Connection.Disconnect();

            RefreshProperties();
        }

        private void CreateRequestProcessesCommand() {
            RequestProcessesCommand = new RelayCommand(RequestProcessesCommandExecute, CanExecuteRequestProcessesCommand);
        }

        public ICommand RequestProcessesCommand {
            get;
            internal set;
        }

        private bool CanExecuteRequestProcessesCommand(Object param) {
            return IsConnected;
        }

        private void RequestProcessesCommandExecute(Object param) {
            RequestProcesses();
        }

        private void CreateKillProcessCommand() {
            KillProcessCommand = new RelayCommand(KillProcessCommandExecute, CanExecuteKillProcessCommand);
        }

        public ICommand KillProcessCommand {
            get;
            internal set;
        }

        private bool CanExecuteKillProcessCommand(Object param) {
            return IsConnected && _selectedPrototype != null && _selectedPrototype.ID > 0;
        }

        private void KillProcessCommandExecute(Object param) {
            RequestKillProcess(_selectedPrototype.ID);
        }

        private void CreateFindProcessCommand()
        {
            FindProcessCommand = new RelayCommand(FindProcessCommandExecute, CanExecuteFindProcessCommand);
        }

        public ICommand FindProcessCommand
        {
            get;
            internal set;
        }

        private void FindProcessCommandExecute(Object param)
        {
            RequestFindProcess(_processToStart);
        }

        private bool CanExecuteFindProcessCommand(Object param)
        {
            return IsConnected && _processToStart != null;
        }

        private void CreateStartProcessCommand()
        {
            StartProcessCommand = new RelayCommand(StartProcessCommandExecute, CanExecuteStartProcessCommand);
        }

        public ICommand StartProcessCommand
        {
            get;
            internal set;
        }

        private void StartProcessCommandExecute(Object param)
        {
            RequestStartProcess(_selectedProcessToStart.ID);
        }

        private bool CanExecuteStartProcessCommand(Object param)
        {
            return IsConnected && _selectedProcessToStart != null && !IsImageTransfering;
        }

        private void CreateOpenImageCommand()
        {
            OpenImageCommand = new RelayCommand(OpenImageCommandExecute, CanExecuteOpenImageCommand);
        }

        public ICommand OpenImageCommand
        {
            get;
            internal set;
        }

        private void OpenImageCommandExecute(Object param)
        {
            var thread = new Thread(new ThreadStart(DisplayFormThread));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private bool CanExecuteOpenImageCommand(Object param)
        {
            return IsConnected && _processImage != null;
        }
        #endregion

        #region Sub-model events
        private void _connection_ConnectionStateChangedEventHandler(ConnectionStatuses status) {
            if (status == ConnectionStatuses.DISCONNECTED) {
                SelectedPrototype = null;
                SelectedProcessToStart = null;

                Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                ProcessCollection.Clear();
                                ProcessToStartCollection.Clear();
                                ProcessToStart = "";
                                ProcessImage = null;
                                _isImageTransfering = false;
                            }));
            }

            ConnectionStatus = status;
            RefreshProperties();
        }

        private void _connection_MessageReceivedEventHandler(Message message) {
            switch (message.Type) {
                case MessageTypes.RESPONSE_PROCESS_END:
                    RequestIcons();
                    break;
                case MessageTypes.RESPONSE_IMAGE:
                    AddProcessIcon(message);
                    break;
                case MessageTypes.RESPONSE_PROCESS:
                    AddProcess(message);
                    break;
                case MessageTypes.RESPONSE_KILL_SUCCESS:
                    RemoveProcess(message);
                    break;
                case MessageTypes.REQUEST_CLOSE:
                    Connection.Disconnect();
                    break;
                case MessageTypes.RESPONSE_PROCESS_TO_START:
                    AddProcessToStart(message);
                    break;
                case MessageTypes.RESPONSE_PROCESS_IMAGE:
                    AddProcessImage(message);
                    break;
                case MessageTypes.RESPONSE_PROCESS_IMAGE_END:
                    SetStartedProcessImage();
                    break;
            }
        }
        #endregion

        #region Methods
        protected override void RequestProcesses() {
            Message message = new Message(MessageTypes.REQUEST_PROCESSES);
            Connection.Send(Connection.Socket, message.Data);
        }

        protected override void RequestKillProcess(int processID) {
            Message message = new Message(MessageTypes.REQUEST_KILL_PROCESS, processID.ToString());
            Connection.Send(Connection.Socket, message.Data);
        }

        protected override void RequestStartProcess(int processID)
        {
            IsImageTransfering = true;
            _transferingTimer.Start();

            Message message = new Message(MessageTypes.REQUEST_START_PROCESS, processID.ToString());
            Connection.Send(Connection.Socket, message.Data);
        }

        protected void RequestFindProcess(string processName) {
            ProcessToStartCollection.Clear();
            Message message = new Message(MessageTypes.REQUEST_SEARCH_PROCESS, processName);
            Connection.Send(Connection.Socket, message.Data);
        }

        protected override void AddProcess(Message message) {
            if (message != null) {
                String[] dataArray = message.Text.Split(';');
                int processID;

                if (dataArray.Count() >= 2 && Int32.TryParse(dataArray[0], out processID)) {

                    ProcessPrototype process = ProcessCollection.FirstOrDefault(x => x.ID == processID);

                    if (process == null) {
                        process = new ProcessPrototype(processID, dataArray[1]);

                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                ProcessCollection.Add(process);
                            }));

                        Connection.Send(Connection.Socket, new Message(MessageTypes.REQUEST_NEXT_PROCESS, process.ID.ToString()));
                    }
                }
            }
        }

        protected override void RemoveProcess(Message message) {
            if (message != null) {
                String data = message.Text;
                int processID;

                if (Int32.TryParse(data, out processID)) {

                    ProcessPrototype process = ProcessCollection.FirstOrDefault(x => x.ID == processID);

                    if (process != null) {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                ProcessCollection.Remove(process);
                            }));
                    }

                    if (SelectedPrototype != null && SelectedPrototype.ID == processID) {
                        SelectedPrototype = null;
                        RefreshProperties();
                    }
                }
            }
        }

        protected override void RequestIcons() {
            Message message = new Message(MessageTypes.REQUEST_ICONS);
            Connection.Send(Connection.Socket, message);
        }

        protected override void AddProcessIcon(Message message) {
            if (message != null) {
                int processID;

                if (Int32.TryParse(Utils.GetTextBetween(message.Text, ConnectionService.PROCESS_START_DELIMITER, ConnectionService.PROCESS_END_DELIMITER), out processID)) {
                    String imageText = message.Text.Substring(0, message.Text.IndexOf(ConnectionService.PROCESS_START_DELIMITER));

                    Bitmap bitmap = Utils.Base64StringToBitmap(imageText);

                    if (bitmap != null) {
                        ProcessPrototype prototype = null;
                        
                        prototype = ProcessCollection.FirstOrDefault(x => x.ID == processID);

                        if (prototype == null) {
                            prototype = ProcessToStartCollection.FirstOrDefault(x => x.ID == processID);
                        }

                        if (prototype != null) {
                            ImageSource imageSource = Utils.BitmapToImageSource(bitmap);
                            bitmap.Dispose();

                            prototype.AddIcon(imageSource);
                        }

                        Connection.Send(Connection.Socket, new Message(MessageTypes.REQUEST_NEXT_ICON, prototype.ID.ToString()));
                    }
                }
            }
        }

        protected void AddProcessToStart(Message message)
        {
            if (message != null)
            {
                String[] dataArray = message.Text.Split(';');
                int processID;

                if (dataArray.Count() >= 2 && Int32.TryParse(dataArray[0], out processID)) {
                    ProcessPrototype process = ProcessToStartCollection.FirstOrDefault(x => x.ID == processID);

                    if (process == null) {
                        process = new ProcessPrototype(processID, dataArray[1]);

                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                ProcessToStartCollection.Add(process);
                            }));

                        Connection.Send(Connection.Socket, new Message(MessageTypes.REQUEST_NEXT_PROCESS_TO_START, process.ID.ToString()));
                    }
                }
            }
        }

        private void AddProcessImage(Message message)
        {
            if (message != null)
            {
                int imagePiece;

                if (Int32.TryParse(Utils.GetTextBetween(message.Text, ConnectionService.PROCESS_START_DELIMITER, ConnectionService.PROCESS_END_DELIMITER), out imagePiece))
                {
                    _processImageString += message.Text.Substring(0, message.Text.IndexOf(ConnectionService.PROCESS_START_DELIMITER));

                    Connection.Send(Connection.Socket, new Message(MessageTypes.REQUEST_PROCESS_IMAGE_NEXT, imagePiece.ToString()));
                }
            }
        }

        private void SetStartedProcessImage()
        {
            Bitmap image = Utils.Base64StringToBitmap(_processImageString);
            ProcessImage = Utils.BitmapToImageSource(image);
            image.Dispose();

            if (ProcessImage != null) {
                var thread = new Thread(new ThreadStart(DisplayFormThread));

                _processImageString = "";

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }

            IsImageTransfering = false;
        }

        private void DisplayFormThread()
        {
            try
            {
                ImageWindow imageWindow = new ImageWindow();

                imageWindow.Dispatcher.Invoke(new Action(() => imageWindow.PART_IMAGE.Source = ProcessImage));
                imageWindow.Show();
                imageWindow.Closed += (s, e) => System.Windows.Threading.Dispatcher.ExitAllFrames();

                System.Windows.Threading.Dispatcher.Run();
            }
            catch (Exception ex) {
                Utils.Log(LogLevels.ERROR, ex.ToString());
            }
        }

        public void RefreshProperties() {
            NotifyPropertyChanged("IsReady");
            NotifyPropertyChanged("IsConnected");
            NotifyPropertyChanged("SelectedPrototype");
        }

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            if (IsImageTransfering) IsImageTransfering = false;
        }
        #endregion
    }
}
