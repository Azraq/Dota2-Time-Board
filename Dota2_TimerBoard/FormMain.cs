using Dota2GSI;
using Dota2GSI.Nodes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Gma.System.MouseKeyHook;
using System.Globalization;

namespace Dota2_TimerBoard
{

    public partial class FormMain : Form
    {

        public static bool FirstState { get; private set; } = true;
        public static GameState PreviousGameState { get; private set; }

        public static TrackBar staticTrackBar;
        private static readonly Random _random = new Random();
        private static readonly float[] _recentHealth = new float[25];
        private static DateTime _lastRunesSound = new DateTime(0);
        private static DateTime _lastHealSound  = new DateTime(0);
        private static int _midasTimer = 0;
        static GameStateListener _gsl;
        static TextBox staticTextBox;
        static Label statictimeLabel;
        static ComboBox staticAudioDevicescbx; private IKeyboardMouseEvents m_GlobalHook;

        private static TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        public FormMain()
        {
            InitializeComponent();
            enumAudioDevices();
            intializeControls();
            Init();
            Subscribe();
        }
        public void Subscribe()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();

       //     m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;
            m_GlobalHook.KeyDown += GlobalHookKeyPress;
        }

        private void GlobalHookKeyPress(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.D2 && e.Modifiers == (Keys.Alt | Keys.Shift))
            {
                PlaySoundFile("roons.wav", true);
            }
            
                if (e.KeyCode == Keys.D3 && e.Modifiers == (Keys.Alt | Keys.Shift))
            {
                PlaySoundFile("unlimitedPower.MP3", true);
            }
            if (e.KeyCode == Keys.D1 && e.Modifiers == (Keys.Alt | Keys.Shift))
            {
                PlaySoundFile("praisethelord.MP3", true);
            }
            if (e.KeyCode == Keys.D4 && e.Modifiers == (Keys.Alt | Keys.Shift))
            {
                PlaySoundFile("joker.MP3", true);
            }
        }

        //private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        //{
        //    Console.WriteLine("MouseDown: \t{0}; \t System Timestamp: \t{1}", e.Button, e.Timestamp);

        //    // uncommenting the following line will suppress the middle mouse button click
        //    // if (e.Buttons == MouseButtons.Middle) { e.Handled = true; }
        //}

        public void Unsubscribe()
        {
        //    m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;
            m_GlobalHook.KeyDown -= GlobalHookKeyPress;

            //It is recommened to dispose it
            m_GlobalHook.Dispose();
        }

        private void enumAudioDevices()
        {
            cbxAudioOutput.DataSource = DirectSoundOut.Devices;
            cbxAudioOutput.DisplayMember = "Description";
            cbxAudioOutput.ValueMember = "Guid";
            cbxAudioOutput.SelectedValue = DirectSoundOut.DSDEVID_DefaultVoicePlayback;

            foreach (var endpoint in DirectSoundOut.Devices)

                     //enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                Console.WriteLine(endpoint.Guid + "   - "+ endpoint.ModuleName + " " + endpoint.Description);
            }
        }


        public void intializeControls()
        {
            try
            {
                staticAudioDevicescbx = cbxAudioOutput;
                staticTrackBar = trackBar;
                var enumerator = new MMDeviceEnumerator();
                var audioEndPoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                string[] ids = audioEndPoint.ID.Split('.');
                staticAudioDevicescbx.SelectedValue = Guid.Parse(ids[4]);
                cbxAudioOutput.SelectedValue = staticAudioDevicescbx.SelectedValue;
            }
            catch { }

            
         //   var pos = this.PointToScreen(lblTime.Location);
         ////   pos = pictureBox1.PointToClient(pos);
         ////   lblTime.Parent = pictureBox1;
         //   lblTime.Location = pos;
         //   lblTime.BackColor = Color.Transparent;

            statictimeLabel = lblTime;
            staticTextBox = textBox1;
            this.DoubleBuffered = true;


        }
        public static string convertToTime(int totalSeconds)
        {
            string time ;

            int seconds = (totalSeconds % 60);
            int minutes = (totalSeconds % 3600) / 60;
            int hours = (totalSeconds % 86400) / 3600;
            int days = (totalSeconds % (86400 * 30)) / 86400;

            time = hours + " : " + minutes + " : " + seconds;
            return time;
        }
        public static int getMinutes(int totalSeconds)
        {
            return (totalSeconds % 3600) / 60;
        }
        public static int getSeconds(int totalSeconds)
        {
            return (totalSeconds % 60);

        }

        private static void FillArray(float[] array, float value)
        {
            // Populate all recent health with 100%.
            for (int i = 0; i < array.Length; i++)
                array[i] = value;
        }
        public static void ClearAll()
        {
            staticTextBox.Invoke((MethodInvoker)(() => { staticTextBox.Clear(); }));
        }
        public static void Writeline(string str)
        {
            staticTextBox.Invoke((MethodInvoker)(() => { staticTextBox.AppendText(str); staticTextBox.AppendText(Environment.NewLine); }));
        }
        public static void WriteTime(string str)
        {
            statictimeLabel.Invoke((MethodInvoker)(() => { statictimeLabel.Text = str; }));
        }

        // Dota 2 GSL -- Start

        public void Init()
        {
            //if (args == null) Console.WriteLine();

            CreateGsifile();

            Process[] pname = Process.GetProcessesByName("Dota2");
            if (pname.Length == 0)
            {
                textBox1.Text += "Dota 2 is not running. Please start Dota 2. \n ";
                //Console.ReadLine();
//Environment.Exit(0);
            }

            _gsl = new GameStateListener(4000);
            _gsl.NewGameState += OnNewGameState;


            if (!_gsl.Start())
            {
                textBox1.Text += "GameStateListener could not start. Try running this program as Administrator. Exiting. \n ";
                //Console.ReadLine();
                //Environment.Exit(0);
            }
            textBox1.Text += "Listening for game integration calls... \n";

            ////Console.WriteLine("Press ESC to quit");
            ////do
            ////{
            ////    while (!(Console.In.Peek() != 0))
            ////    {
            ////        Thread.Sleep(1000);  
            ////    }
            ////} while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
        static void OnNewGameState(GameState gs)
        {
            try
            {
                // Console.Clear();
                // Console.WriteLine("Press ESC to quit");
                ClearAll();
                StringBuilder str = new StringBuilder();
                str.AppendLine("Current Dota version: " + gs.Provider.Version);
                str.AppendLine("Your Name: " + gs.Player.Name);
                string heroname = gs.Hero.Name.Remove(0, 14).Replace('_', ' ');
                heroname = textInfo.ToTitleCase(heroname);
                str.AppendLine("Hero: " + heroname);
               // str.AppendLine("Game State: " + gs.Map.GameState);
               // str.AppendLine("Game Time: " + gs.Map.GameTime);
                Writeline(str.ToString());
                WriteTime(convertToTime(gs.Map.ClockTime));



                for (int i = 0; i < gs.Abilities.Count; i++)
                {
                    //Writeline("Ability {0} = {1}", i, gs.Abilities[i].Name);
                }
                //Writeline("First slot inventory: " + gs.Items.GetInventoryAt(0).Name);
                //Writeline("Second slot inventory: " + gs.Items.GetInventoryAt(1).Name);
                //Writeline("Third slot inventory: " + gs.Items.GetInventoryAt(2).Name);
                //Writeline("Fourth slot inventory: " + gs.Items.GetInventoryAt(3).Name);
                //Writeline("Fifth slot inventory: " + gs.Items.GetInventoryAt(4).Name);
                //Writeline("Sixth slot inventory: " + gs.Items.GetInventoryAt(5).Name);

                //if (gs.Items.InventoryContains("item_blink"))
                //    Writeline("You have a blink dagger");
                //else
                //    Writeline("You DO NOT have a blink dagger");

                try
                {
                    // Check if there is no on-going match.
                    if (gs.Map.MatchID == -1)
                    {

                        PreviousGameState = null;
                        ClearAll();
                        Writeline("Waiting For Match To Start");
                        return;
                    }
                }
                catch (ArgumentException ex)
                {
                    return;
                }


                // On Game Start
                if (PreviousGameState == null) // Check if the previous match's id is the same as the current one.
                {
                    //  UnmanagedMemoryStream audioStream = Properties.Resources.ResourceManager.GetStream(gs.Hero.Name);
                    //  if (audioStream != null) // Check if the audio stream actually exists; without this, it would play windows default alert (\a).
                    if (gs.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME)
                        PlaySoundFile("its_me_mario.wav",true);
                }

                // Runes Timer
                // The sound is limited to 1 per 2 minutes since it will otherwise repeat about 5 times ever 5 minutes.
                if (gs.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS
                    && getMinutes(gs.Map.ClockTime) == 0
                    && getSeconds(gs.Map.ClockTime) == 0
                   )
                {
                    if (DateTime.Now - _lastRunesSound > TimeSpan.FromMilliseconds(750))
                    {
                        PlaySoundFile("roons.wav",true);
                        _lastRunesSound = DateTime.Now;
                    }
                }

                // On Player Died
                if (PreviousGameState != null && !gs.Hero.IsAlive && PreviousGameState.Hero.IsAlive)
                {
                    FillArray(_recentHealth, 100f);
                }

                if (PreviousGameState != null && !gs.Hero.IsAlive && PreviousGameState.Hero.IsAlive && gs.Hero.SecondsToRespawn == 1)
                {
                    PlaySoundFile("herewegoagain.wav",true);
                }

                // On Player Win/Lose
                if (gs.Map.Win_team != PlayerTeam.None)
                {
                    if (PreviousGameState.Map.Win_team == PlayerTeam.None) // This is to make sure the code is only run once.
                        if (gs.Player.Team == gs.Map.Win_team)
                            PlaySoundFile("vivon",true);
                        else
                            PlaySoundFile("wefuckinglost.wav",true);
                }

                int healthPercentage = gs.Hero.HealthPercent;

                // On Player Heal
                if (PreviousGameState != null && gs.Previously.Hero.Health != 0 && // Previous State health must be higher than 0 (not dead/wraith king passive).
                    gs.Hero.Health - PreviousGameState.Hero.Health > 200 && // Hero must have gained at least 200 health.
                    healthPercentage - PreviousGameState.Hero.HealthPercent > 5 && // Hero must have gained at least 5% of its health.
                    _random.NextDouble() <= 0.33
                    )
                {
                    //if (DateTime.Now - _lastHealSound > TimeSpan.FromMilliseconds(750))
                    //{
                    PlaySoundFile("eel.wav",true);

                    _lastHealSound = DateTime.Now;
                    //}
                }

                // Midas Check
                foreach (Item item in gs.Items.Inventory)
                {
                    if (item.Name == "item_hand_of_midas") // We're doing it inverted from what DatGuy1 did since we might want to add timers for other items.
                    {
                        if (item.Cooldown > 0)
                            _midasTimer = 0;

                        else
                        {
                            if (_midasTimer >= 50)
                            {
                                PlaySoundFile("useyourmidas.wav",true);
                                _midasTimer -= 225;
                            }

                            _midasTimer++;
                        }
                    }
                }

                PreviousGameState = gs;





            }
            catch(Exception e)
            {
                //MessageBox.Show(e.Message);
            }

            _gsl.NewGameState -= OnNewGameState;
            Thread.Sleep(500);

            _gsl.NewGameState += OnNewGameState;
        }

        private static void CreateGsifile()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            if (regKey != null)
            {
                string gsifolder = regKey.GetValue("SteamPath") +
                                   @"\steamapps\common\dota 2 beta\game\dota\cfg\gamestate_integration";
                Directory.CreateDirectory(gsifolder);
                string gsifile = gsifolder + @"\gamestate_integration_testGSI.cfg";
                if (File.Exists(gsifile))
                    return;

                string[] contentofgsifile =
                {
                    "\"Dota 2 Integration Configuration\"",
                    "{",
                    "    \"uri\"           \"http://localhost:4000\"",
                    "    \"timeout\"       \"5.0\"",
                    "    \"buffer\"        \"0.1\"",
                    "    \"throttle\"      \"0.1\"",
                    "    \"heartbeat\"     \"30.0\"",
                    "    \"data\"",
                    "    {",
                    "        \"provider\"      \"1\"",
                    "        \"map\"           \"1\"",
                    "        \"player\"        \"1\"",
                    "        \"hero\"          \"1\"",
                    "        \"abilities\"     \"1\"",
                    "        \"items\"         \"1\"",
                    "    }",
                    "}",

                };

                File.WriteAllLines(gsifile, contentofgsifile);
            }
            else
            {
                Console.WriteLine("Registry key for steam not found, cannot create Gamestate Integration file");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void radToggleSwitch2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            PlaySoundFile("praisethelord.mp3", true);

        }

        private static void PlaySound(UnmanagedMemoryStream sound)
        {
            new SoundPlayer(sound).Play();
        }

        
        private static void PlaySoundFile(string fileName, bool isDefaultPath)
        {
            try
            {
                DirectSoundOut sound = new DirectSoundOut((Guid)staticAudioDevicescbx.SelectedValue);

                string fullPath = "";
                if (isDefaultPath)
                    fullPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Audio\\" + fileName);
                else
                    fullPath = fileName;

                var player = new AudioFileReader(fullPath);
                player.Volume = staticTrackBar.Value / 100f;
                sound.Init(player);
                sound.Play();
            }
            catch (Exception e)
            {
                // does nothing for now
            }
        }

        private void cbxAudioOutput_SelectionChangeCommitted(object sender, EventArgs e)
        {
            staticAudioDevicescbx.SelectedValue = cbxAudioOutput.SelectedValue;
        }

        private void btnPlay_respawn_Click(object sender, EventArgs e)
        {

            PlaySoundFile("herewegoagain.wav", true);
        }

        private void btnPlay_Roons_Click(object sender, EventArgs e)
        {
            PlaySoundFile("roons.wav", true);
        }

        private void btnPlay_2minRoon_Click(object sender, EventArgs e)
        {

            PlaySoundFile("roons.wav", true);
        }

        private void btnPlay_neutralStack_Click(object sender, EventArgs e)
        {

            PlaySoundFile("roons.wav", true);
        }

        private void bunifuFlatButton5_Click(object sender, EventArgs e)
        {

            PlaySoundFile("eel.wav", true);
        }

        private void bunifuFlatButton1_Click(object sender, EventArgs e)
        {

            PlaySoundFile("useyourmidas.wav", true);
        }

        private void bunifuFlatButton3_Click(object sender, EventArgs e)
        {

            PlaySoundFile("praisethelord.MP3", true);
        }

        private void bunifuFlatButton2_Click(object sender, EventArgs e)
        {

            PlaySoundFile("unlimitedPower.MP3", true);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Unsubscribe();
        }

        private void bunifuGradientPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void bunifuFlatButton4_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void bunifuFlatButton6_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            staticTrackBar.Value = trackBar.Value;
        }

        // GSL - END

    }

   
}
