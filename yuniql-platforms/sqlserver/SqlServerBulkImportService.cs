﻿using System.Data.SqlClient;
using System.Data;
using System.IO;
using Yuniql.Extensibility;
using Yuniql.Extensibility.BulkCsvParser;

//https://github.com/22222/CsvTextFieldParser
namespace Yuniql.SqlServer
{
    ///<inheritdoc/>
    public class SqlServerBulkImportService : IBulkImportService
    {
        private string _connectionString;
        private readonly ITraceService _traceService;

        ///<inheritdoc/>
        public SqlServerBulkImportService(ITraceService traceService)
        {
            this._traceService = traceService;
        }

        ///<inheritdoc/>
        public void Initialize(string connectionString)
        {
            this._connectionString = connectionString;
        }

        ///<inheritdoc/>
        public void Run(
            IDbConnection connection,
            IDbTransaction transaction,
            string fileFullPath,
            string bulkSeparator = null,
            int? bulkBatchSize = null,
            int? commandTimeout = null)
        {
            //check if a non-default dbo schema is used
            var schemaName = "dbo";
            var tableName = Path.GetFileNameWithoutExtension(fileFullPath);
            if (tableName.IndexOf('.') > 0)
            {
                schemaName = tableName.Split('.')[0];
                tableName = tableName.Split('.')[1];
            }

            //read csv file and load into data table
            var dataTable = ParseCsvFile(fileFullPath, bulkSeparator);

            //save the csv data into staging sql table
            BulkCopyWithDataTable(
                connection,
                transaction,
                schemaName,
                tableName,
                dataTable,
                bulkBatchSize,
                commandTimeout);
        }

        private DataTable ParseCsvFile(
            string csvFileFullPath,
            string bulkSeparator = null)
        {
            if (string.IsNullOrEmpty(bulkSeparator))
                bulkSeparator = ",";

            var csvDatatable = new DataTable();
            using (var csvReader = new CsvTextFieldParser(csvFileFullPath))
            {
                csvReader.Separators = (new string[] { bulkSeparator });
                csvReader.HasFieldsEnclosedInQuotes = true;

                string[] csvColumns = csvReader.ReadFields();
                foreach (string csvColumn in csvColumns)
                {
                    var dataColumn = new DataColumn(csvColumn);
                    dataColumn.AllowDBNull = true;
                    csvDatatable.Columns.Add(dataColumn);
                }

                while (!csvReader.EndOfData)
                {
                    string[] fieldData = csvReader.ReadFields();
                    for (int i = 0; i < fieldData.Length; i++)
                    {
                        if (fieldData[i] == "" || fieldData[i] == "NULL")
                        {
                            fieldData[i] = null;
                        }
                    }
                    csvDatatable.Rows.Add(fieldData);
                }
            }
            return csvDatatable;
        }

        private void BulkCopyWithDataTable(
            IDbConnection connection,
            IDbTransaction transaction,
            string schemaName,
            string tableName,
            DataTable dataTable,
            int? bulkBatchSize = null,
            int? commandTimeout = null)
        {
            using (var sqlBulkCopy = new SqlBulkCopy(connection as SqlConnection, SqlBulkCopyOptions.Default, transaction as SqlTransaction))
            {
                sqlBulkCopy.DestinationTableName = $"[{schemaName}].[{tableName}]";
                sqlBulkCopy.BulkCopyTimeout = commandTimeout.HasValue ? commandTimeout.Value : DEFAULT_CONSTANTS.COMMAND_TIMEOUT_SECS;
                sqlBulkCopy.BatchSize = bulkBatchSize.HasValue ? bulkBatchSize.Value : DEFAULT_CONSTANTS.BULK_BATCH_SIZE;
                sqlBulkCopy.EnableStreaming = true;
                sqlBulkCopy.SqlRowsCopied += SqlBulkCopy_SqlRowsCopied;
                foreach (var column in dataTable.Columns)
                {
                    sqlBulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());
                }
                sqlBulkCopy.WriteToServer(dataTable);
            }
        }

        private void SqlBulkCopy_SqlRowsCopied(object sender, SqlRowsCopiedEventArgs e)
        {
            _traceService.Info($"SqlServerBulkImportService copied {e.RowsCopied} rows");
        }
    }
}

