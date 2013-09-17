﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Customer_App
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// The kettler input device class
        /// </summary>
        private Kettler_X7_Lib.Classes.Kettler_X7 m_pKettlerX7;

        /// <summary>
        /// The networking client that connects to the server
        /// </summary>
        private Kettler_X7_Lib.Networking.Client m_pNetworkClient;

        /// <summary>
        /// The list of values that were received from the bike
        /// </summary>
        private List<Kettler_X7_Lib.Objects.Value> m_pValueList;

        /// <summary>
        /// The data class used for saving and receiving data from the server
        /// </summary>
        private Classes.Data m_pData;

        public Form1()
        {
            InitializeComponent();

            // Initialize stuff
            m_pValueList = new List<Kettler_X7_Lib.Objects.Value>();
            m_pKettlerX7 = new Kettler_X7_Lib.Classes.Kettler_X7();
            m_pNetworkClient = new Kettler_X7_Lib.Networking.Client();
            m_pData = new Classes.Data(m_pNetworkClient);
        }

        /// <summary>
        /// Returns the networking client
        /// </summary>
        /// <returns></returns>
        public Kettler_X7_Lib.Networking.Client getClient()
        {
            return m_pNetworkClient;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize networking client
            m_pNetworkClient.connect("127.0.0.1", Kettler_X7_Lib.Classes.Global.TCPSERVER_PORT);
            m_pNetworkClient.DataReceived += m_pNetworkClient_DataReceived;

            // Initialize bike
            if (!m_pKettlerX7.connect(null, "127.0.0.1", 49326, Kettler_X7_Lib.Classes.Kettler_X7.Source.SOURCE_SIMULATOR))
            {
                Kettler_X7_Lib.Classes.GUI.throwError("Kan geen verbinding met de fiets maken!");
            }
            else
            {
                m_pKettlerX7.startParsingValues(1000);

                m_pKettlerX7.ValuesParsed += pKetlerX7_ValuesParsed;
            }

            // Populate listbox with commands
            foreach (Kettler_X7_Lib.Classes.Kettler_X7.Command nCommand in Enum.GetValues(typeof(Kettler_X7_Lib.Classes.Kettler_X7.Command)))
            {
                lstCommands.Items.Add(nCommand);
            }
        }

        void m_pNetworkClient_DataReceived(object sender, Kettler_X7_Lib.Networking.Server.DataReceivedEventArgs e)
        {
            // Data from server
        }

        void pKetlerX7_ValuesParsed(object sender, Kettler_X7_Lib.Classes.Kettler_X7.ValuesParsedEventArgs e)
        {
            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblPulseValue, delegate
            {
                lblPulseValue.Text = e.Value.Pulse.ToString();
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblRPMValue, delegate
            {
                lblRPMValue.Text = e.Value.RPM.ToString();
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblSpeedValue, delegate
            {
                lblSpeedValue.Text = (e.Value.Speed / 10) + " km/h";
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblDistanceValue, delegate
            {
                lblDistanceValue.Text = ((double)e.Value.Distance / 10) + " kilometer";
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblReqPowerValue, delegate
            {
                lblReqPowerValue.Text = e.Value.RequestedPower.ToString();
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblActPowerValue, delegate
            {
                lblActPowerValue.Text = e.Value.ActualPower.ToString();
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblEnergyValue, delegate
            {
                lblEnergyValue.Text = e.Value.Energy + " Kj";
            });

            Kettler_X7_Lib.Classes.GUI.safelyUpdateControl(lblTimeValue, delegate
            {
                lblTimeValue.Text = e.Value.Time.ToString();
            });

            m_pData.addData(e.Value);
        }
        
        /// <summary>
        /// When this form is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_pKettlerX7.onClose();
        }

        /// <summary>
        /// When the user wants to send a command to the bike
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendCommand_Click(object sender, EventArgs e)
        {
            if (lstCommands.SelectedItem == null || !(lstCommands.SelectedItem is Kettler_X7_Lib.Classes.Kettler_X7.Command))
            {
                Kettler_X7_Lib.Classes.GUI.throwError("Incorrecte waarde geselecteerd!");
                return;
            }

            Kettler_X7_Lib.Classes.Kettler_X7.Command nCommand = (Kettler_X7_Lib.Classes.Kettler_X7.Command)lstCommands.SelectedItem;

            if (nCommand.ToString().StartsWith("CHANGE") && txtCommand.TextLength == 0)
            {
                Kettler_X7_Lib.Classes.GUI.throwError("Geen waarde opgegeven, dit commando vereist een waarde!");
                return;
            }

            // If there is a return value, we should display it
            if (nCommand.ToString().StartsWith("RETURN"))
            {
                System.Diagnostics.Debug.WriteLine(m_pKettlerX7.sendReturnCommand(nCommand, (txtCommand.TextLength == 0 ? null : txtCommand.Text)));
                return;   
            }

            if (!m_pKettlerX7.sendCommand(nCommand, (txtCommand.TextLength == 0 ? null : txtCommand.Text)))
            {
                Kettler_X7_Lib.Classes.GUI.throwError("Kon commando niet verzenden!");
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_pData.sendData();
        }
    }
}
