﻿/*

Copyright Robert Vesse 2009-12
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager
{
    public partial class StartPage : Form
    {
        public StartPage(IGraph recent, IGraph faves)
        {
            InitializeComponent();

            this.FillConnectionList(recent, this.lstRecent);
            this.FillConnectionList(faves, this.lstFaves);
        }

        private void StartPage_Load(object sender, EventArgs e)
        {

        }

        private void FillConnectionList(IGraph config, ListBox lbox)
        {
             SparqlParameterizedString query = new SparqlParameterizedString();
            query.Namespaces.AddNamespace("rdfs", new Uri(NamespaceMapper.RDFS));
            query.Namespaces.AddNamespace("dnr", new Uri(ConfigurationLoader.ConfigurationNamespace));

            query.CommandText = "SELECT * WHERE { ?obj a " + ConfigurationLoader.ClassGenericManager + " . OPTIONAL { ?obj rdfs:label ?label } }";
            query.CommandText += " ORDER BY DESC(?obj)";

            SparqlResultSet results = config.ExecuteQuery(query) as SparqlResultSet;
            if (results != null)
            {
                foreach (SparqlResult r in results)
                {
                    QuickConnect connect;
                    if (r.HasValue("label") && r["label"] != null)
                    {
                        connect = new QuickConnect(config, r["obj"], r["label"].ToString());
                    }
                    else
                    {
                        connect = new QuickConnect(config, r["obj"]);
                    }
                    lbox.Items.Add(connect);
                }
            }
            lbox.DoubleClick += new EventHandler((sender, args) =>
                {
                    QuickConnect connect = lbox.SelectedItem as QuickConnect;
                    if (connect != null)
                    {
                        try
                        {
                            IGenericIOManager manager = connect.GetConnection();
                            StoreManagerForm storeManager = new StoreManagerForm(manager);
                            storeManager.MdiParent = Program.MainForm;
                            storeManager.Show();

                            //Add to Recent Connections
                            Program.MainForm.AddRecentConnection(manager);

                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error Opening Connection " + connect.ToString() + ":\n" + ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                });
        }

        private void btnNewConnection_Click(object sender, EventArgs e)
        {
            NewConnectionForm newConn = new NewConnectionForm();
            if (newConn.ShowDialog() == DialogResult.OK)
            {
                IGenericIOManager manager = newConn.Connection;
                StoreManagerForm storeManager = new StoreManagerForm(manager);
                storeManager.MdiParent = Program.MainForm;
                storeManager.Show();

                //Add to Recent Connections
                Program.MainForm.AddRecentConnection(manager);

                this.Close();
            }
        }

        private void chkAlwaysShow_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowStartPage = this.chkAlwaysShow.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
