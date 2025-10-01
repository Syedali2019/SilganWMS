using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.DataLogic;
using Rossell.Common;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;
using Rossell.BusinessEntity;

namespace Rossell.BusinessLogic
{
    public class ServiceBusinessLogic : IDisposable
    {
        private static HttpClient _httpClient;
        private string baseUrl = ConfigurationManager.AppSettings["BASEWEBAPIURL"].ToString();
        private string username = ConfigurationManager.AppSettings["USERNAME"].ToString();
        private string password = ConfigurationManager.AppSettings["PASSWORD"].ToString();
        
        private string appName = "Rossell";

        public ServiceBusinessLogic()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }
        public string MasterLabelRepack(decimal masterLabelID, decimal quantity, string serialNumber)
        {
            var response = new HttpResponseMessage();
            string newSerialNumber = string.Empty;
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);

                string urlMasterLabelRepack = "Inventory/Disposition/MasterLabelRepack";
                var endpoint = "?id=" + masterLabelID + "&qty=" + quantity;
                MasterLabel masterLabel = new MasterLabel();
                response = _httpClient.GetAsync($"{baseUrl}/{urlMasterLabelRepack}/{endpoint}").GetAwaiter().GetResult();                
                

                response.EnsureSuccessStatusCode();
                var masterLabelRepackResponse = JsonConvert.DeserializeObject<IQMS.Entities.Lib.API.IqWebResponse>(response.Content.ReadAsStringAsync().Result);
                if (masterLabelRepackResponse.Status == IQMS.Entities.Lib.API.ResponseStatus.Ok)
                {
                    string json = response.Content.ReadAsStringAsync().Result;
                    json = json.Replace("data", "MasterLabelModel");
                    RootObject obj = JsonConvert.DeserializeObject<RootObject>(json);
                    MasterLabelModel masterLabelModel = obj.MasterLabelModel.SingleOrDefault(ms => ms.SerialNo != serialNumber);
                    newSerialNumber= masterLabelModel.SerialNo;
                }
                return newSerialNumber;
            }
            catch (Exception ex)
            {
                return newSerialNumber;
            }
        }

        public long AddInventoryLocation(long arINVTID, long locationID, string lotNumber, long standardID)
        {
            long fgMultiID = 0;
            var response = new HttpResponseMessage();
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);

                string urlAddInLocation = "Manufacturing/Inventory/AddInventoryLocation";
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlAddInLocation}", new StringContent(JsonConvert.SerializeObject(new { arinvtId = arINVTID, locId = locationID, lotno = lotNumber, associateWithStandardId = standardID, defaultLocation = false, nonConformId = DBNull.Value, allocatable = true }), Encoding.UTF8, "application/json"))).Result;
                response.EnsureSuccessStatusCode();
                string json = response.Content.ReadAsStringAsync().Result;
                json = json.Replace("data", "FgMultiModel");
                RootObjectFGMulti obj = JsonConvert.DeserializeObject<RootObjectFGMulti>(json);
                fgMultiID = obj.FgMultiModel.fgMultiId;
                return fgMultiID;
            }
            catch (Exception ex)
            {
                return fgMultiID;
            }
        }

        public WebApiResponse AddLocation(long arinvtID, long locationID, string lotNo)
        {
            WebApiResponse webAPIResponse = new WebApiResponse();
            var response = new HttpResponseMessage();
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));



                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);
                string urlCreateAssyTrackLabor = "AssemblyData/FinalAssembly/AddLocation";

                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlCreateAssyTrackLabor}", new StringContent(JsonConvert.SerializeObject(new { arinvtId = arinvtID, locId = locationID, lotNo = lotNo }), Encoding.UTF8, "application/json"))).Result;
                if (response.IsSuccessStatusCode == true && response.ReasonPhrase == "OK")
                {
                    string json = response.Content.ReadAsStringAsync().Result;
                    var addLocationResult = JsonConvert.DeserializeObject<IQMS.Entities.Lib.API.IqWebResponse>(response.Content.ReadAsStringAsync().Result);
                    if (addLocationResult.Status == IQMS.Entities.Lib.API.ResponseStatus.Ok)
                    {
                        webAPIResponse.DidSucceed = true;
                        webAPIResponse.Message = "";
                        webAPIResponse.Status = true;
                    }                    
                }
                else
                {
                    string json = response.Content.ReadAsStringAsync().Result;
                    webAPIResponse.DidSucceed = false;
                    webAPIResponse.Message = "Location doesn't add";
                    webAPIResponse.Status = false;
                }

                return webAPIResponse;

            }
            catch (Exception ex)
            {
                webAPIResponse.DidSucceed = false;
                webAPIResponse.Message = ex.Message;
                webAPIResponse.Status = false;
                return webAPIResponse;
            }
        }

        public WebApiResponse AddItemToLocation(long arinvtID, long fgmultiID, decimal quantity, long standardID, int transCode, DateTime transDate, MasterLabelDetail masterLabel)
        {
            WebApiResponse webAPIResponse = new WebApiResponse();
            var response = new HttpResponseMessage();
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));



                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);
                string urlCreateAssyTrackLabor = "Inventory/TransactionLocation/AddItemToLocation";
                var endpoint = "?arInvtId=" + arinvtID + "&fgMultiId=" + fgmultiID + "&quantity=" + quantity + "&rgQuantity=0" + "&transCode=" + transCode + "&transDate=" + transDate + "&reason=" + "" + "&standardID=" + standardID + "&backflush=true";

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add("Id", masterLabel.MASTER_LABEL_ID.ToString());
                parameters.Add("SerialNo", masterLabel.SERIAL.ToString());
                parameters.Add("Qty", masterLabel.TOTAL_QUANTITY.ToString());
                parameters.Add("DispoDate", string.Format(masterLabel.DISPO_DATE, "yyyy/MM/dd hh:mm:ss"));
                parameters.Add("ParentId", "0");
                parameters.Add("ParentSerial", "");
                parameters.Add("FgLotNo", masterLabel.LOT_NO.ToString());
                parameters.Add("PrintDate", DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"));

                MultipartFormDataContent formData = new MultipartFormDataContent();
                HttpContent DictionaryItems = new FormUrlEncodedContent(parameters);
                formData.Add(DictionaryItems, "InventoryMasterLabel");

                //response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlCreateAssyTrackLabor}", new StringContent(JsonConvert.SerializeObject(new { arInvtId = arinvtID, fgMultiId = fgmultiID, quantity = quantity, rgQuantity = 0, transCode = transCode, transDate = transDate, standardID = standardID, backflush = true }), Encoding.UTF8, "application/json"))).Result;
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlCreateAssyTrackLabor}{endpoint}", formData)).Result;
                if (response.IsSuccessStatusCode == true && response.ReasonPhrase == "OK")
                {
                    string json = response.Content.ReadAsStringAsync().Result;
                    var addLocationResult = JsonConvert.DeserializeObject<IQMS.Entities.Lib.API.IqWebResponse>(response.Content.ReadAsStringAsync().Result);
                    if (addLocationResult.Status == IQMS.Entities.Lib.API.ResponseStatus.Ok)
                    {
                        webAPIResponse.DidSucceed = true;
                        webAPIResponse.Message = "";
                        webAPIResponse.Status = true;
                    }
                }
                else
                {
                    string json = response.Content.ReadAsStringAsync().Result;
                    webAPIResponse.DidSucceed = false;
                    webAPIResponse.Message = "Item to Location doesn't add";
                    webAPIResponse.Status = false;
                }

                return webAPIResponse;

            }
            catch (Exception ex)
            {
                webAPIResponse.DidSucceed = false;
                webAPIResponse.Message = ex.Message;
                webAPIResponse.Status = false;
                return webAPIResponse;
            }
        }

        public WebApiResponse MoveToLocation(long arINVTID, long sourceFgMultiId, long targetFgMultiId, decimal quantity, BusinessEntity.MasterLabel masterLabel)
        {
            WebApiResponse webAPIResponse = new WebApiResponse();
            long fgMultiID = 0;
            var response = new HttpResponseMessage();
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);
                string urlAddInLocation = "Inventory/TransactionLocation/MoveToLocation";               
                var endpoint = "?arInvtId=" + arINVTID + "&sourceFgMultiId=" + sourceFgMultiId + "&targetFgMultiId=" + targetFgMultiId + "&quantity=" + quantity + "&rgQuantity= 0" + "&reason=" + "" + "&origDateIn=" + DateTime.Now.ToString("yyyy/MM/dd") + "&nonConformId=null";
                
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add("Id", masterLabel.MASTER_LABEL_ID.ToString());
                parameters.Add("SerialNo", masterLabel.SERIAL.ToString());
                parameters.Add("Qty", masterLabel.QUANTITY.ToString());
                parameters.Add("DispoDate", string.Format(masterLabel.DISPO_DATE, "yyyy/MM/dd hh:mm:ss"));
                parameters.Add("ParentId", "0");
                parameters.Add("ParentSerial", "");
                parameters.Add("FgLotNo", masterLabel.FG_LOTNO.ToString());
                parameters.Add("PrintDate", DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"));

                MultipartFormDataContent formData = new MultipartFormDataContent();
                HttpContent DictionaryItems = new FormUrlEncodedContent(parameters);
                formData.Add(DictionaryItems, "InventoryMasterLabel");                
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlAddInLocation}{endpoint}", formData)).Result;
                string json = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    webAPIResponse.DidSucceed = true;
                    webAPIResponse.Message = "";
                    webAPIResponse.Status = true;
                }
                else
                {
                    string error = response.Content.ReadAsStringAsync().Result;
                    json = json.Replace("data", "webApiResponse");
                    RootObjectResponseError obj = JsonConvert.DeserializeObject<RootObjectResponseError>(json);

                    webAPIResponse.DidSucceed = false;
                    webAPIResponse.Message = obj.iqmsServiceError.FriendlyMessage;
                    webAPIResponse.Status = false;
                }
                return webAPIResponse;
            }
            catch (Exception ex)
            {
                webAPIResponse.DidSucceed = false;
                webAPIResponse.Message = ex.Message;
                webAPIResponse.Status = false;
                return webAPIResponse;
            }
        }

        public bool MoveToLocationFromMasterLabel(string serialNumber, long locationID)
        {            
            var response = new HttpResponseMessage();
            try
            {
                bool retValue = false;
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);
                string urlMoveToLocation = "Inventory/TransactionLocation/MoveToLocationFromMasterLabel";                              
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlMoveToLocation}", new StringContent(JsonConvert.SerializeObject(new { labelSerial = serialNumber, newLoc = locationID}), Encoding.UTF8, "application/json"))).Result;

                string json = response.Content.ReadAsStringAsync().Result;
                json = json.Replace("data", "webApiResponse");
                RootObjectResponse obj = JsonConvert.DeserializeObject<RootObjectResponse>(json);
                if (obj != null && obj.webApiResponse != null)
                {
                    retValue = obj.webApiResponse.DidSucceed;
                }
                else
                {
                    retValue = false;
                }
                return retValue;               
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateMasterLabel(BusinessEntity.Master_Label masterLabel, BusinessEntity.FGMULTI fgMulti, string locDesc)
        {            
            var response = new HttpResponseMessage();
            try
            {
                if (masterLabel != null)
                {
                    _httpClient = null;
                    _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                    _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);
                    string urlUpateMasterLabel = "Labels/PrintLabel/UpdateMasterLabel";

                    Dictionary<string, string> parameters = new Dictionary<string, string>();

                    parameters.Add("ArcustoId", "0");
                    parameters.Add("ArinvtId", masterLabel.ARINVT_ID.ToString());
                    parameters.Add("Boxno", masterLabel.BOX_NO.ToString());
                    parameters.Add("BoxId", masterLabel.BOX_ID.ToString());
                    parameters.Add("Class", masterLabel.CLASS.ToString());
                    parameters.Add("Countryorg", null);
                    parameters.Add("DayPartId", "0");
                    parameters.Add("Descrip", masterLabel.DESCRIPTION);
                    parameters.Add("Descrip2", masterLabel.DESCRIPTION2);
                    parameters.Add("DispoDate", string.Format(masterLabel.DISPO_DATE, "yyyy/MM/dd hh:mm:ss"));
                    parameters.Add("DispoScan", masterLabel.DISPO_DATE);
                    parameters.Add("DockId", null);
                    parameters.Add("Eqno", null);
                    parameters.Add("FgmultiId", masterLabel.FGMULTI_ID.ToString());
                    parameters.Add("FgLotno", masterLabel.FG_LOTNO.ToString());
                    parameters.Add("Id", masterLabel.ID.ToString());
                    parameters.Add("InvCuser1", masterLabel.CUSER1.ToString());
                    parameters.Add("InvCuser2", masterLabel.CUSER2.ToString());
                    parameters.Add("InvCuser3", null);
                    parameters.Add("InvCuser4", null);
                    parameters.Add("InvCuser5", null);
                    parameters.Add("InvCuser6", null);
                    parameters.Add("InvCuser7", null);
                    parameters.Add("InvCuser8", null);
                    parameters.Add("InvCuser9", null);
                    parameters.Add("InvCuser10", null);
                    parameters.Add("InvNuser1", "0");
                    parameters.Add("InvNuser2", "0");
                    parameters.Add("InvNuser3", "0");
                    parameters.Add("InvNuser4", "0");
                    parameters.Add("InvNuser5", "0");
                    parameters.Add("InvNuser6", "0");
                    parameters.Add("InvNuser7", "0");
                    parameters.Add("InvNuser8", "0");
                    parameters.Add("InvNuser9", "0");
                    parameters.Add("InvNuser10", "0");
                    parameters.Add("IsPallet", null);
                    parameters.Add("IsSkid", null);
                    parameters.Add("Itemno", masterLabel.ITEM_NO);
                    parameters.Add("LastSndopId", "0");
                    parameters.Add("Linefeed", null);
                    parameters.Add("LmLabelsId", masterLabel.LM_LABEL_ID.ToString());
                    parameters.Add("LocDesc", locDesc);
                    parameters.Add("LotDate", string.Format(masterLabel.LOT_DATE, "yyyy/MM/dd hh:mm:ss"));
                    parameters.Add("MaxTagno", null);
                    parameters.Add("Mfgno", masterLabel.MFG_NO);
                    parameters.Add("MinTagno", null);
                    parameters.Add("NextSndopDispatchId", "0");
                    parameters.Add("NextSndopId", "0");
                    parameters.Add("Noship", null);
                    parameters.Add("Orderno", masterLabel.ORDER_NO);
                    parameters.Add("OrdDetailId", masterLabel.ORDER_DETAIL_ID.ToString());
                    parameters.Add("OrigSysdate", string.Format(masterLabel.ORIFINAL_SYSDATE, "yyyy/MM/dd hh:mm:ss"));
                    parameters.Add("OrigUserName", masterLabel.ORIGINAL_USERNAME.ToString());
                    parameters.Add("ParentId", "0");
                    parameters.Add("Pci11z", null);
                    parameters.Add("Pci12z", null);
                    parameters.Add("Pci13z", null);
                    parameters.Add("Pci14z", null);
                    parameters.Add("Pci15z", null);
                    parameters.Add("Pci16z", null);
                    parameters.Add("Pci17z", null);
                    parameters.Add("PkgAkaId", "0");
                    parameters.Add("PkgAkaItemno", null);
                    parameters.Add("Pono", masterLabel.PO_NO.ToString());
                    parameters.Add("Pressno", masterLabel.PRESS_NO.ToString());
                    parameters.Add("PrintDate", string.Format(masterLabel.PRINT_DATE, "yyyy/MM/dd hh:mm:ss"));
                    parameters.Add("PrintQty", masterLabel.Print_Qty.ToString());
                    parameters.Add("ProcessLogin", null);
                    parameters.Add("ProcessShiftId", "0");
                    parameters.Add("ProdDate", string.Format(DateTime.Now.ToString(), "yyyy/MM/dd hh:mm:ss"));
                    parameters.Add("PsTicketDtlId", "0");
                    parameters.Add("PsTicketRelId", "0");
                    parameters.Add("Qty", masterLabel.QUANTITY.ToString());
                    parameters.Add("RepackedMasterLabelId", "0");
                    parameters.Add("Reserveloc", null);
                    parameters.Add("Rev", "");
                    parameters.Add("Scanned", null);
                    parameters.Add("SegMan", null);
                    parameters.Add("Serial", masterLabel.SERIAL);
                    parameters.Add("ShipmentDtlId", "0");
                    parameters.Add("Shipping", null);
                    parameters.Add("ShipDockLocationsId", "0");
                    parameters.Add("SndopDispatchId", "0");
                    parameters.Add("StandardId", "0");
                    parameters.Add("SysDate", string.Format(masterLabel.SYS_DATE, "yyyy/MM/dd hh:mm:ss"));
                    parameters.Add("Trackno", null);
                    parameters.Add("UpcCode", null);
                    parameters.Add("UpcCode2", null);
                    parameters.Add("UserName", masterLabel.USERNAME);
                    parameters.Add("Verified", null);
                    parameters.Add("VinNo", null);
                    parameters.Add("VinWoId", "0");
                    parameters.Add("VmiConsumed", null);
                    parameters.Add("Volume", "0");
                    parameters.Add("Weight", "0");
                    parameters.Add("WorkorderId", "0");
                    parameters.Add("IsLinkedToSerial", null);
                    parameters.Add("TranslogId", "0");

                    string json2 = JsonConvert.SerializeObject(parameters);
                    MultipartFormDataContent formData = new MultipartFormDataContent();
                    HttpContent DictionaryItems = new FormUrlEncodedContent(parameters);
                    formData.Add(DictionaryItems, "MasterLabelRow");
                    response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlUpateMasterLabel}", formData)).Result;
                    string json = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public bool ReprintMasterLabel(long masterLabelID, string printerName)
        {
            var response = new HttpResponseMessage();
            bool retValue= false;
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);

                string urlMasterLabelReprint = "Inventory/ScanId/ReprintMasterLabel";                
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlMasterLabelReprint}", new StringContent(JsonConvert.SerializeObject(new { masterLabelId = masterLabelID, printerName = printerName}), Encoding.UTF8, "application/json"))).Result;
                
                string json = response.Content.ReadAsStringAsync().Result;
                json = json.Replace("data", "webApiResponse");
                RootObjectResponse obj = JsonConvert.DeserializeObject<RootObjectResponse>(json);
                if (obj != null && obj.webApiResponse != null)
                {
                    retValue = obj.webApiResponse.DidSucceed;
                }
                else
                {
                    retValue = false;
                }
                return retValue;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ReprintBetweenMasterLabel(string serialFrom, string serialTo)
        {
            var response = new HttpResponseMessage();
            bool retValue = false;
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                string token = authToken.AuthToken;
                token = token.Substring(token.Length - 36, 36);
                _httpClient.DefaultRequestHeaders.Add("AuthToken", token);

                string urlMasterLabelReprint = "Inventory/SerialTracking/ReprintBetween";
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlMasterLabelReprint}", new StringContent(JsonConvert.SerializeObject(new { serialFrom = serialFrom, serialTo = serialTo }), Encoding.UTF8, "application/json"))).Result;

                string json = response.Content.ReadAsStringAsync().Result;
                json = json.Replace("data", "webApiResponse");
                RootObjectResponse obj = JsonConvert.DeserializeObject<RootObjectResponse>(json);
                if (obj != null && obj.webApiResponse != null)
                {
                    retValue = obj.webApiResponse.DidSucceed;
                }
                else
                {
                    retValue = false;
                }
                return retValue;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ReprintSerialLabel(string serialNumber, long masterLabelID, string printerName)
        {
            var response = new HttpResponseMessage();
            bool retValue = false;
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];                
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);

                string urlMasterLabelReprint = "Labels/PrintLabel/ReprintSerial";
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlMasterLabelReprint}", new StringContent(JsonConvert.SerializeObject(new { serialNo = serialNumber, masterLabelId = masterLabelID, printerName= printerName }), Encoding.UTF8, "application/json"))).Result;

                string json = response.Content.ReadAsStringAsync().Result;
                json = json.Replace("data", "webApiResponse");
                RootObjectResponse obj = JsonConvert.DeserializeObject<RootObjectResponse>(json);
                if (obj != null && obj.webApiResponse != null)
                {
                    retValue = obj.webApiResponse.DidSucceed;
                }
                else
                {
                    retValue = false;
                }
                return retValue;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<Printer> GetPrinterList()
        {
            var response = new HttpResponseMessage();
            List<Printer> printerList =new List<Printer>();
            string newSerialNumber = string.Empty;
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);

                string urlPrinterList = "Labels/PrintLabel/PrinterList";               
                MasterLabel masterLabel = new MasterLabel();
                response = _httpClient.GetAsync($"{baseUrl}/{urlPrinterList}").GetAwaiter().GetResult();


                response.EnsureSuccessStatusCode();
                var printerListResponse = JsonConvert.DeserializeObject<IQMS.Entities.Lib.API.IqWebResponse>(response.Content.ReadAsStringAsync().Result);
                if (printerListResponse.Status == IQMS.Entities.Lib.API.ResponseStatus.Ok)
                {
                    string json = response.Content.ReadAsStringAsync().Result;
                    json = json.Replace("data", "Printer");
                    RootObjectPrinter obj = JsonConvert.DeserializeObject<RootObjectPrinter>(json);
                    printerList = obj.Printer;
                }
                return printerList;
            }
            catch (Exception ex)
            {
                return printerList;
            }
        }


        public bool DeleteInventoryLocation(long fgMultiId)
        {
            var response = new HttpResponseMessage();
            bool retValue = false;
            try
            {
                _httpClient = null;
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                AuthTokenModel authToken = (AuthTokenModel)HttpContext.Current.Session["AUTHTOKEN"];
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authToken.AuthToken);

                string urlDeleteInventoryLocation = "Manufacturing/Inventory/DeleteInventoryLocation";
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}{urlDeleteInventoryLocation}", new StringContent(JsonConvert.SerializeObject(new { fgMultiId = fgMultiId }), Encoding.UTF8, "application/json"))).Result;

                string json = response.Content.ReadAsStringAsync().Result;
                json = json.Replace("data", "webApiResponse");
                RootObjectResponse obj = JsonConvert.DeserializeObject<RootObjectResponse>(json);
                if (obj != null && obj.webApiResponse != null)
                {
                    retValue = obj.webApiResponse.DidSucceed;
                }
                else
                {
                    retValue = false;
                }
                return retValue;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void GetAuthToken(LoginModel loginModel)
        {
            var response = new HttpResponseMessage();

            try
            {
                var authTokenModel = new AuthTokenModel();
                string _loginAPI = "User/Login";                
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}/{_loginAPI}", new StringContent(JsonConvert.SerializeObject(loginModel), Encoding.UTF8, "application/json"))).Result;

                response.EnsureSuccessStatusCode();
                authTokenModel = JsonConvert.DeserializeObject<AuthTokenModel>(response.Content.ReadAsStringAsync().Result);                
                _httpClient.DefaultRequestHeaders.Add("AuthToken", authTokenModel.AuthToken);
            }
            catch (Exception ex)
            {
            }
        }

        public AuthTokenModel UserAuthentication(LoginModel loginModel)
        {
            var response = new HttpResponseMessage();

            try
            {
                var authTokenModel = new AuthTokenModel();
                string _loginAPI = "User/Login";
                response = Task.Run(() => _httpClient.PostAsync($"{baseUrl}/{_loginAPI}", new StringContent(JsonConvert.SerializeObject(loginModel), Encoding.UTF8, "application/json"))).Result;

                response.EnsureSuccessStatusCode();

                authTokenModel = JsonConvert.DeserializeObject<AuthTokenModel>(response.Content.ReadAsStringAsync().Result);

                return authTokenModel;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }


    public class LoginModel
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    public class AuthTokenModel
    {
        [JsonProperty("AuthToken")]
        public string AuthToken { get; set; }

        public AuthTokenModel()
        {
            AuthToken = string.Empty;
        }
    }

    public class MasterLabel
    {
        [JsonProperty("Id")]
        public int ID { get; set; }

        [JsonProperty("SerialNo")]
        public string SerialNumber { get; set; }
    }

    public class RootObject
    {
        public List<MasterLabelModel> MasterLabelModel { get; set; }
    }

    public class RootObjectFGMulti
    {
        public FGMULTIModel FgMultiModel { get; set; }
    }

    public class RootObjectResponse
    {
        public WebApiResponse webApiResponse { get; set; }
    }

    public class RootObjectPrinter
    {
        public List<Printer> Printer { get; set; }
    }

    public class RootObjectResponseError
    {
        public ServiceMessage iqmsServiceError { get; set; }
    }
}
