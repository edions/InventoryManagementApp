﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace InventoryApp.Services
{
    public class PointOfSale
    {
        private readonly SqlConnection con = ConnectionManager.GetConnection();
        private static readonly HashSet<string> generatedIds = new HashSet<string>();

        public void InitializeComboBox(ComboBox comboBox)
        {
            comboBox.Items.Add(new ComboBoxItem { Value = 10, Description = "10% off" });
            comboBox.Items.Add(new ComboBoxItem { Value = 15, Description = "15% off" });
            comboBox.Items.Add(new ComboBoxItem { Value = 30, Description = "30% off" });
            comboBox.Items.Add(new ComboBoxItem { Value = 50, Description = "50% off" });
        }

        public void LoadCartItems(ListBox listBox)
        {
            try
            {
                con.Open();

                string selectQuery = "SELECT Name, Price, Quantity FROM Cart";

                using (SqlCommand command = new SqlCommand(selectQuery, con))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        listBox.Items.Clear();

                        while (reader.Read())
                        {
                            string name = reader["Name"].ToString();
                            int price = Convert.ToInt32(reader["Price"]);
                            int quantity = Convert.ToInt32(reader["Quantity"]);

                            string item = $"{quantity} x {name} - ${price}";
                            listBox.Items.Add(item);
                        }
                    }
                }

                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading cart items: " + ex.Message);
            }
        }

        public void CalculateDiscount(string totalText, object selectedItem, Label labelDiscount, Label labelTotalAfterDiscount)
        {
            int total = Convert.ToInt32(totalText);
            double discountAmount = 0;

            if (selectedItem is ComboBoxItem selectedComboBoxItem)
            {
                double discountPercent = selectedComboBoxItem.Value;
                discountAmount = total * (discountPercent / 100);
            }

            double totalAfterDiscount = total - discountAmount;
            labelDiscount.Text = (0 - discountAmount).ToString();
            labelTotalAfterDiscount.Text = totalAfterDiscount.ToString();
        }

        public bool ProcessTransaction(string totalText, string cashText, object selectedItem, string transactionId)
        {
            int total = Convert.ToInt32(totalText);
            int cash = string.IsNullOrWhiteSpace(cashText) ? 0 : Convert.ToInt32(cashText);
            double discountPercent = 0;

            if (selectedItem is ComboBoxItem selectedComboBoxItem)
            {
                discountPercent = selectedComboBoxItem.Value;
            }

            // Calculate the discount amount
            double discountAmount = total * (discountPercent / 100);

            // Validate if there is enough cash
            if (cash < (total - discountAmount))
            {
                MessageBox.Show("Not enough cash to complete the transaction.");
                return false;
            }

            double change = cash - (total - discountAmount);
            DateTime currentDate = DateTime.Now;

            // Save the transaction data to the database
            try
            {
                con.Open();
                string insertQuery = "INSERT INTO [Transaction] (TransactionId, Total, Cash, DiscountPercent, DiscountAmount, [Change], Date) VALUES (@TransactionId, @Total, @Cash, @DiscountPercent, @DiscountAmount, @Change, @Date)";

                using (SqlCommand command = new SqlCommand(insertQuery, con))
                {
                    command.Parameters.AddWithValue("@TransactionId", transactionId);
                    command.Parameters.AddWithValue("@Total", total);
                    command.Parameters.AddWithValue("@Cash", cash);
                    command.Parameters.AddWithValue("@DiscountPercent", discountPercent);
                    command.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                    command.Parameters.AddWithValue("@Change", change);
                    command.Parameters.AddWithValue("@Date", currentDate);
                    command.ExecuteNonQuery();
                }

                // Delete the data from the Cart table after successful transaction
                string deleteQuery = "DELETE FROM [Cart]";
                using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, con))
                {
                    deleteCommand.ExecuteNonQuery();
                }

                con.Close();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving the transaction: " + ex.Message);
                return false;
            }
        }

        public void InsertTransactionItems(ListBox listBox, string transactionId)
        {
            con.Open();
            string insertQuery = "INSERT INTO Orders (TransactionId, Name, Price, Quantity) VALUES (@TransactionId, @Name, @Price, @Quantity)";

            using (SqlCommand insertCommand = new SqlCommand(insertQuery, con))
            {
                foreach (var item in listBox.Items)
                {
                    string[] parts = item.ToString().Split(new string[] { " x ", " - $" }, StringSplitOptions.None);
                    string name = parts[1];
                    decimal price = decimal.Parse(parts[2]);
                    int quantity = int.Parse(parts[0]);

                    insertCommand.Parameters.Clear();
                    insertCommand.Parameters.AddWithValue("@TransactionId", transactionId);
                    insertCommand.Parameters.AddWithValue("@Name", name);
                    insertCommand.Parameters.AddWithValue("@Price", price);
                    insertCommand.Parameters.AddWithValue("@Quantity", quantity);
                    insertCommand.ExecuteNonQuery();
                }
            }

            con.Close();
        }
    }

    public class ComboBoxItem
    {
        public double Value { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }
}