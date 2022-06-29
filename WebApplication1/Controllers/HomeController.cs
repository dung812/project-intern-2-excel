using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.OleDb;
using System.IO;


namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString);

        OleDbConnection Econ;

        private void ExcelConn(string filepath)

        {
            string constr = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 12.0 Xml;HDR=YES;""", filepath);
            Econ = new OleDbConnection(constr);
        }

        private void InsertExceldata(string fileepath, string filename)

        {
            string fullpath = Server.MapPath("/excelfolder/") + filename;
            ExcelConn(fullpath);
            string query = string.Format("Select * from [{0}]", "Sheet1$");
            OleDbCommand Ecom = new OleDbCommand(query, Econ);
            Econ.Open();
            DataSet ds = new DataSet();
            OleDbDataAdapter oda = new OleDbDataAdapter(query, Econ);
            Econ.Close();
            oda.Fill(ds);
            DataTable dt = ds.Tables[0];
            SqlBulkCopy objbulk = new SqlBulkCopy(con);
            objbulk.DestinationTableName = "tbl_registration";
            objbulk.ColumnMappings.Add("Email", "Email");
            objbulk.ColumnMappings.Add("Password", "Password");
            objbulk.ColumnMappings.Add("Name", "Name");
            objbulk.ColumnMappings.Add("Address", "Address");
            objbulk.ColumnMappings.Add("City", "City");
            con.Open();
            objbulk.WriteToServer(dt);
            con.Close();

        }

        public ActionResult Index()
        {
            return View();
        }        
        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            // Read Data From Excel File & render in view
            DataTable dtSheet = new DataTable();
            DataSet ExcelData = new DataSet();

            string path = Server.MapPath("~/excelfolder/");
            string filePath = string.Empty;
            string extension = string.Empty;
            if (file != null)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                filePath = path + Path.GetFileName(file.FileName);
                extension = Path.GetExtension(file.FileName);
                file.SaveAs(filePath);

            }
            string connectionString = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=""Excel 12.0 Xml;HDR=YES;""", filePath);
            using (OleDbConnection connExcel = new OleDbConnection(connectionString))
            {
                using (OleDbCommand cmdExcel = new OleDbCommand())
                {
                    using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                    {
                        cmdExcel.Connection = connExcel;
                        connExcel.Open();
                        DataTable dtExcelSchema;
                        dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                        string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                        connExcel.Close();

                        connExcel.Open();
                        cmdExcel.CommandText = "SELECT * FROM [" + sheetName + "]";
                        odaExcel.SelectCommand = cmdExcel;
                        odaExcel.Fill(dtSheet);
                        connExcel.Close();
                    }
                }
            }

            // Save Excel file into database
            string filename = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string filepath = "/excelfolder/" + filename;
            file.SaveAs(Path.Combine(Server.MapPath("/excelfolder"), filename));
            InsertExceldata(filepath, filename);


            ViewBag.SuccessUpload = "Success upload new Data";
            ExcelData.Tables.Add(dtSheet);
            return View(ExcelData);
        }
    }
}