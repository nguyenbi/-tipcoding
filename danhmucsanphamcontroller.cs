using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BatDongSan.Controllers;
using BatDongSan.Helper.Common;
using BatDongSan.Helper.Utils;
using BatDongSan.Utils.Paging;
using System.Text;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BatDongSan.Helper;
using System.Data;
using ClosedXML.Excel;
using BatDongSan.Models.QuanLyTraSua;
using BatDongSan.Models.CuDan;
using NPOI.HSSF.UserModel.Contrib;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.Util;
using Worldsoft.Mvc.Web.Util;
using NPOI;
using System.Net.Mail;
using BatDongSan.Models.QuanLyThiTruong;
using BatDongSan.Models.KinhDoanh;
namespace BatDongSan.Controllers.QuanLyThiTruong
{
    public class DanhMucSanPhamController : ApplicationController
    {
        //
        // GET: /DanhMucLoaiPhieu/
        private readonly string MCV = "DanhMucSanPham";
        private bool? permission;
        private lqQLTTDataContext context = new lqQLTTDataContext();
        private static int defaultPageSize = 20;
        private string duongDan;
        public NguyenLieuModel model;
        private string mimeType;
        public tbl_QLTT_DanhMucSanPham record;
        private StringBuilder sb = new StringBuilder();
        public ActionResult Index(int? page, int? pageSize, string qSearch, string tuNgay, string denNgay, int? rowNumber, string maLoaiThuoc)
        {
            #region Role user
            permission = GetPermission(MCV, BangPhanQuyen.QuyenXem);
            if (!permission.HasValue)
                return View("LogIn");
            if (!permission.Value)
                return View("AccessDenied");
            #endregion

            BindmaLoaiThuoc(string.Empty);
            ViewBag.Users = GetUser().manv;
            TempData["Params"] = pageSize + "," + qSearch + "," + tuNgay + "," + denNgay + "," + maLoaiThuoc;
            int currentPageIndex = page.HasValue ? page.Value : 1;
            defaultPageSize = pageSize ?? 20;
            int? tongSoDong = 0;

            DateTime? fromDate = null;
            if (!string.IsNullOrEmpty(tuNgay))
            {
                fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            DateTime? toDate = null;
            if (!string.IsNullOrEmpty(denNgay))
            {
                toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture).AddDays(1);
            }

            var lstDinhKhoan = context.sp_QLTT_DanhMucSanPham_Index(qSearch, fromDate, toDate, currentPageIndex, defaultPageSize, maLoaiThuoc).ToList();
            try
            {
                ViewBag.Count = lstDinhKhoan[0].tongSoDong;
                tongSoDong = lstDinhKhoan[0].tongSoDong;
            }
            catch
            {
                ViewBag.Count = 0;
            }

            //Quyền admin - start
            ViewBag.QuyenAdmin = GetQuyenAdminThiTruong(GetUser().manv);
            //Quyền admin - end

            if (Request.IsAjaxRequest())
            {
                ViewBag.Ajax = true;
                return PartialView("ViewIndex", lstDinhKhoan.ToPagedList(currentPageIndex, defaultPageSize, true, tongSoDong));
            }

            return View(lstDinhKhoan.ToPagedList(currentPageIndex, defaultPageSize, true, tongSoDong));
        }

        public ActionResult Create(string id, string maThuoc)
        {
            #region Role user
            permission = GetPermission(MCV, BangPhanQuyen.QuyenThem);
            if (!permission.HasValue)
                return View("LogIn");
            if (!permission.Value)
                return View("AccessDenied");
            #endregion
            try
            {
                tbl_QLTT_DanhMucSanPham model = new tbl_QLTT_DanhMucSanPham();
                model.ngayLap = DateTime.Now;
                model.nguoiLap = HoVaTenNhanVien(GetUser().manv);
                model.maSanPham = IdGeneratorDungChung(GetMaxPhieu(), "DMSP");
                var danhSachThuoc = from cp in context.tbl_QLTT_DanhMucThuocs
                                    orderby cp.maThuoc

                                    select new
                                    {
                                        maThuoc = Convert.ToString(cp.maThuoc),
                                        tenThuoc = cp.tenThuoc
                                    };
                ViewData["danhSachThuoc"] = new SelectList(danhSachThuoc, "maThuoc", "tenThuoc", model.maThuoc);
                BindmaLoaiThuoc(string.Empty);
                return View(model);

            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View("Failed");
            }
        }
        public void BindmaLoaiThuoc(string maThuoc)
        {
            var lstDanhMucThuocs = context.tbl_QLTT_DanhMucThuocs.OrderBy(d => d.maThuoc).ToList();
            lstDanhMucThuocs.Insert(0, new tbl_QLTT_DanhMucThuoc { maThuoc = string.Empty, tenThuoc = "[Chọn Loại thuốc]" });
            ViewBag.lstDanhMucThuocs = new SelectList(lstDanhMucThuocs, "maThuoc", "tenThuoc", maThuoc);

        }
        public ActionResult Edit(string id, string maThuoc)
        {
            #region Role user
            permission = GetPermission(MCV, BangPhanQuyen.QuyenSua);
            if (!permission.HasValue)
                return View("LogIn");
            if (!permission.Value)
                return View("AccessDenied");
            #endregion

            try
            {
                tbl_QLTT_DanhMucSanPham model = new tbl_QLTT_DanhMucSanPham();
                model = context.tbl_QLTT_DanhMucSanPhams.Where(d => d.maSanPham == id).FirstOrDefault();
                model.nguoiLap = HoVaTenNhanVien(model.nguoiLap);
                var danhSachThuoc = from cp in context.tbl_QLTT_DanhMucThuocs
                                    orderby cp.maThuoc

                                    select new
                                    {
                                        maThuoc = Convert.ToString(cp.maThuoc),
                                        tenThuoc = cp.tenThuoc
                                    };
                ViewData["danhSachThuoc"] = new SelectList(danhSachThuoc, "maThuoc", "tenThuoc", model.maThuoc);
                BindmaLoaiThuoc(string.Empty);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
                return View("Failed");
            }
        }

        public ActionResult Details(string id, string maThuoc)
        {
            #region Role user
            permission = GetPermission(MCV, BangPhanQuyen.QuyenXemChiTiet);
            if (!permission.HasValue)
                return View("LogIn");
            if (!permission.Value)
                return View("AccessDenied");
            #endregion

            try
            {
                tbl_QLTT_DanhMucSanPham model = new tbl_QLTT_DanhMucSanPham();
                model = context.tbl_QLTT_DanhMucSanPhams.Where(d => d.maSanPham == id).FirstOrDefault();
                model.nguoiLap = HoVaTenNhanVien(model.nguoiLap);
                var danhSachThuoc = from cp in context.tbl_QLTT_DanhMucThuocs
                                    orderby cp.maThuoc

                                    select new
                                    {
                                        maThuoc = Convert.ToString(cp.maThuoc),
                                        tenThuoc = cp.tenThuoc
                                    };
                ViewData["danhSachThuoc"] = new SelectList(danhSachThuoc, "maThuoc", "tenThuoc", model.maThuoc);
                BindmaLoaiThuoc(string.Empty);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
                return View("Failed");
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(FormCollection collection, IEnumerable<HttpPostedFileBase> fileDinhKems)
        {
            try
            {
                BindDataToSave(collection, true);
                UpLoadMultipleFile(collection, fileDinhKems, record.maSanPham);
                context.tbl_QLTT_DanhMucSanPhams.InsertOnSubmit(record);
                context.SubmitChanges();
                return RedirectToAction("Edit", "DanhMucSanPham", new { id = record.maSanPham });
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
                return View("Failed");
            }
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(FormCollection collection, IEnumerable<HttpPostedFileBase> fileDinhKems)
        {
            try
            {

                BindDataToSave(collection, false);
                UpLoadMultipleFile(collection, fileDinhKems, record.maSanPham);
                context.SubmitChanges();
                return RedirectToAction("Edit", "DanhMucSanPham", new { id = record.maSanPham });
            }
            catch (Exception ex)
            {

                ViewBag.Message = ex.ToString();
                return View("Failed");
            }
        }

        public void BindDataToSave(FormCollection col, bool isCreate)
        {
            if (isCreate == true)
            {
                record = new tbl_QLTT_DanhMucSanPham();
                record.nguoiLap = GetUser().manv;
                record.ngayLap = DateTime.Now;
                record.maSanPham = col.Get("maSanPham");
                //IdGeneratorDungChung(GetMaxPhieu(), "NL");
            }
            else
            {
                record = context.tbl_QLTT_DanhMucSanPhams.Where(d => d.maSanPham == col.Get("maSanPham")).FirstOrDefault();
            }
            record.tenSanPham = col.Get("tenSanPham");
            record.maThuoc = col.Get("maThuoc");
            record.donGia = String.IsNullOrEmpty(col.Get("donGia")) ? 0 : Convert.ToDouble(col.Get("donGia"));
            record.soThung = String.IsNullOrEmpty(col.Get("soThung")) ? 0 : Convert.ToDouble(col.Get("soThung"));
            record.diemTrenThung = String.IsNullOrEmpty(col.Get("diemTrenThung")) ? 0 : Convert.ToInt32(col.Get("diemTrenThung"));

            record.maNhomLeft = col.Get("maNhomLeft");
            // delete chi tiet cu

            //var lstChiTiets = context.tbl_QLTT_DanhMucSanPham_ChiTiets.Where(d => d.maThuoc == record.maThuoc).ToList();
            //if (lstChiTiets != null && lstChiTiets.Count > 0)
            //{
            //    context.tbl_QLTT_DanhMucSanPham_ChiTiets.DeleteAllOnSubmit(lstChiTiets);
            //}
            ////Insert chi tiết
            //string[] tenDoiTuong = col.GetValues("tenDoiTuong");
            //List<tbl_QLTT_DanhMucSanPham_ChiTiet> chiTiets = new List<tbl_QLTT_DanhMucSanPham_ChiTiet>();
            //tbl_QLTT_DanhMucSanPham_ChiTiet ct;
            //if (tenDoiTuong != null && tenDoiTuong.Count() > 0)
            //{
            //    for (int i = 0; i < tenDoiTuong.Count(); i++)
            //    {
            //        ct = new tbl_QLTT_DanhMucSanPham_ChiTiet();
            //        ct.maThuoc = record.maThuoc;
            //        ct.tenDoiTuong = tenDoiTuong[i];
            //        ct.dacTri = String.IsNullOrEmpty(col.GetValues("dacTri")[i]) ? "0" : Convert.ToString(col.GetValues("dacTri")[i]);
            //        ct.lieuLuong = String.IsNullOrEmpty(col.GetValues("lieuLuong")[i]) ? "0" : Convert.ToString(col.GetValues("lieuLuong")[i]);
            //        ct.cachDung = String.IsNullOrEmpty(col.GetValues("cachDung")[i]) ? "0" : Convert.ToString(col.GetValues("cachDung")[i]);
            //        chiTiets.Add(ct);
            //    }
            //}
            //if (chiTiets != null && chiTiets.Count > 0)
            //{
            //    context.tbl_QLTT_DanhMucSanPham_ChiTiets.InsertAllOnSubmit(chiTiets);
            //}

        }

        public string GetMaxPhieu()
        {
            try
            {
                string lastID = context.tbl_QLTT_DanhMucSanPhams.OrderByDescending(d => d.maSanPham).Select(d => d.maSanPham).FirstOrDefault();
                return lastID;
            }
            catch
            {
                return string.Empty;
            }
        }
        [HttpPost]
        public ActionResult Delete(string[] maSoXe, FormCollection collection)
        {
            #region Role user
            permission = GetPermission(MCV, BangPhanQuyen.QuyenXoa);
            if (!permission.HasValue)
                return View("LogIn");
            if (!permission.Value)
                return View("AccessDenied");
            #endregion
            try
            {
                if (maSoXe != null)
                {

                    for (int i = 0; i < maSoXe.Count(); i++)
                    {
                        var listFileHA = context.tbl_QLTT_DanhMucSanPhams.Where(d => d.maSanPham == maSoXe[i]).FirstOrDefault();
                        if (listFileHA != null)
                        {
                            UploadHelper fileHelper = new UploadHelper();
                            context.tbl_QLTT_DanhMucSanPhams.DeleteOnSubmit(listFileHA);
                        }
                        //
                        //var listXoa = context.tbl_QLTT_DanhMucSanPham_ChiTiets.Where(d => d.maThuoc == maSoXe[i]).ToList();
                        //if (listXoa != null)
                        //{
                        //    context.tbl_QLTT_DanhMucSanPham_ChiTiets.DeleteAllOnSubmit(listXoa);
                        //}

                        //var files = context.GetTable<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>().Where(d => maSoXe.Contains(d.identification) && d.controller == GeneralUtil.GetRouteData().Controller);
                        //context.GetTable<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>().DeleteAllOnSubmit(files);


                    }
                    context.SubmitChanges();
                    return RedirectToAction("Index");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
                return View("Failed");
            }
        }
        #region Đính kèm nhiêu file
        private void UpLoadMultipleFile(FormCollection collection, IEnumerable<HttpPostedFileBase> files, string id)
        {
            try
            {
                files = files.Where(s => s != null).OrderBy(o => o.FileName);
                string[] nameacceptable = collection.GetValues("nameaccept");
                string[] thumbnails = collection.GetValues("thumbnail");
                List<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem> taiLieus = new List<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>();
                foreach (var file in files)
                {
                    if (file != null)
                    {
                        BatDongSan.Models.KinhDoanh.tbl_FileDinhKem taiLieu = new BatDongSan.Models.KinhDoanh.tbl_FileDinhKem();
                        var date = DateTime.Now.ToString("yyyyMMdd-HHMMss");
                        string filePath = "/FileUploads/DanhMucSanPham/DanhMucSanPham/";

                        //Tạo tên mới cho file upload
                        string Generatedname = date.ToString() + file.FileName;
                        Directory.CreateDirectory(filePath);
                        var filePathOriginal = Server.MapPath(filePath);
                        if (nameacceptable.Contains(file.FileName))
                        {
                            string savedFileName = Path.Combine(filePathOriginal, Generatedname);
                            taiLieu.controller = GeneralUtil.GetRouteData().Controller;// Get Tên controller
                            taiLieu.Action = GeneralUtil.GetRouteData().Action; //Get Tên Action
                            taiLieu.savedFileName = date.ToString() + file.FileName;
                            taiLieu.maNguoiUpLoad = GetUser().manv;
                            taiLieu.ngayLap = DateTime.Now;
                            taiLieu.tenNguoiUpLoad = (string)Session["TenNhanVien"];
                            taiLieu.originalFileName = file.FileName;
                            taiLieu.savedFileName = Generatedname;
                            taiLieu.taiLieuURL = filePath + Generatedname;
                            taiLieu.identification = id;
                            if (Array.Exists(thumbnails, s => s.Contains(file.FileName)) && file.ContentType.Contains("image") == false)
                            {
                                string thumbnail = thumbnails.Where(s => s.Contains(file.FileName)).First();
                                //Tách giá trị value thumbnail thành 2 phần 1-Tên file, 2-Tên đường dẫn thumbnail
                                taiLieu.thumbnailURL = Regex.Split(thumbnail, "-SplitPoint-").Last();
                            }
                            else
                            {
                                if (file.ContentType.Contains("image"))
                                {
                                    taiLieu.thumbnailURL = "/FileUploads/DanhMucSanPham/DanhMucSanPham/" + Generatedname;
                                }
                            }
                            file.SaveAs(savedFileName);
                            taiLieus.Add(taiLieu);
                            int Index = Array.IndexOf(nameacceptable, file.FileName);
                            Array.Clear(nameacceptable, Index, 1);
                        }
                    }
                }
                context.GetTable<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>().InsertAllOnSubmit(taiLieus);
                context.SubmitChanges();
            }
            catch
            {
            }

        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Download(int id)
        {
            try
            {
                var taiLieu = context.GetTable<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>().Where(s => s.id == id).FirstOrDefault();
                string savedFileName = Path.Combine("/FileUploads/DanhMucSanPham/DanhMucSanPham/", taiLieu.savedFileName);
                return new DownloadResult { VirtualPath = savedFileName, FileDownloadName = taiLieu.originalFileName };
            }
            catch
            {
                return Json("");
            }
        }

        [HttpPost]
        public ActionResult DeleteFile(int id)
        {
            try
            {
                var fileDinhkem = context.GetTable<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>().Where(s => s.id == id).FirstOrDefault();
                context.GetTable<BatDongSan.Models.KinhDoanh.tbl_FileDinhKem>().DeleteOnSubmit(fileDinhkem);
                context.SubmitChanges();
                System.IO.File.Delete(Server.MapPath(fileDinhkem.taiLieuURL));
                return Json(String.Empty);
            }
            catch
            {
                return View();
            }
        }
        #endregion
        public ActionResult CheckMaPhieu(string maSanPham)
        {
            try
            {
                var hasvalue = string.Empty;
                var tblPhieu = context.tbl_QLTT_DanhMucSanPhams.Where(d => d.maSanPham == maSanPham).FirstOrDefault();
                if (tblPhieu != null)
                {
                    hasvalue = "isHave";
                }
                return Json(hasvalue);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public FileResult Downloads()
        {
            string savedFileName = Path.Combine("/UserFiles/Template/", "DMSANPHAMDONGXANH.xlsx");
            return File(savedFileName, "multipart/form-data", "DMSANPHAMDONGXANH.xlsx");
        }

        [HttpPost]
        public ActionResult ImportExcelData(string excelPath)
        {


            string fileName = String.Empty;
            try
            {
                string[] supportedFiles = { ".xlsx", ".xls" };
                HttpPostedFileBase File;
                File = Request.Files[0];
                if (File.ContentLength > 0)
                {
                    string extension = Path.GetExtension(File.FileName);
                    bool exist = Array.Exists(supportedFiles, element => element == extension);
                    if (exist == false)
                    {
                        return Json(new { success = false });
                    }
                    else
                    {
                        var date = DateTime.Now.ToString("yyyyMMdd-HHMMss");
                        string savedLocation = "/UserFiles/Upload/";
                        Directory.CreateDirectory(savedLocation);
                        var filePath = Server.MapPath(savedLocation);
                        fileName = date.ToString() + File.FileName;
                        string savedFileName = Path.Combine(filePath, fileName);
                        File.SaveAs(savedFileName);

                        ExcelDataProcessing excelDataProcessor = new ExcelDataProcessing(savedFileName);
                        DataTable dt = excelDataProcessor.GetDataTableWorkSheet("DMSP");

                        foreach (DataRow row in dt.Rows)
                        {
                            if (String.IsNullOrEmpty(row[1].ToString()))
                            {
                                break;
                            }
                            string maSanPham = Convert.ToString(row["maSanPham"].ToString());
                            var checkLoaiPhieu = context.tbl_QLTT_DanhMucSanPhams.Where(d => d.maSanPham == maSanPham).FirstOrDefault();
                            if (checkLoaiPhieu != null)
                            {
                                checkLoaiPhieu.maThuoc = Convert.ToString(row["maHang"].ToString());
                                checkLoaiPhieu.tenSanPham = Convert.ToString(row["tenSanPham"].ToString());
                                checkLoaiPhieu.soThung = Convert.ToDouble(row["thung"].ToString());
                                checkLoaiPhieu.donGia = Convert.ToDouble(row["donGia"].ToString());
                                checkLoaiPhieu.diemTrenThung = Convert.ToInt32(row["diemTrenThung"].ToString());
                                context.SubmitChanges();
                            }
                            else
                            {
                                record = new tbl_QLTT_DanhMucSanPham();
                                record.nguoiLap = GetUser().manv;
                                record.ngayLap = DateTime.Now;
                                record.maSanPham = Convert.ToString(row["maSanPham"].ToString());
                                record.maThuoc = Convert.ToString(row["maHang"].ToString());
                                record.maNhomLeft = record.maThuoc;
                                record.tenSanPham = Convert.ToString(row["tenSanPham"].ToString());
                                record.soThung = Convert.ToDouble(row["thung"].ToString());
                                record.donGia = Convert.ToDouble(row["donGia"].ToString());
                                record.diemTrenThung = Convert.ToInt32(row["diemTrenThung"].ToString());
                                context.tbl_QLTT_DanhMucSanPhams.InsertOnSubmit(record);
                                context.SubmitChanges();
                            }

                        }
                    }

                }
                return Json(string.Empty);
            }
            catch (Exception ex)
            {
                if (!String.IsNullOrEmpty(fileName))
                {
                    System.IO.File.Delete(Server.MapPath("/UserFiles/Upload/" + fileName));
                }
                return Json(ex.Message);
            }
        }
        public ActionResult ExportExcelFile(int? page, int? pageSize, string qSearch, string tuNgay, string denNgay, int? rowNumber, string maLoaiThuoc)
        {
            try
            {
                #region Role user
                permission = GetPermission(MCV, BangPhanQuyen.QuyenXem);
                if (!permission.HasValue)
                    return View("LogIn");
                if (!permission.Value)
                    return View("AccessDenied");
                #endregion

                DateTime? fromDate = null;
                if (!string.IsNullOrEmpty(tuNgay))
                {
                    fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                DateTime? toDate = null;
                if (!string.IsNullOrEmpty(denNgay))
                {
                    toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture).AddDays(1);
                }

                //Tạo file Excel
                string templatePath = Server.MapPath("/UserFiles/Template/DanhMucSanPham.xlsx");
                XLWorkbook wb = new XLWorkbook(Path.Combine(templatePath));

                var record = context.sp_QLTT_DanhMucSanPham_Index(qSearch, fromDate, toDate, 1, 200000000, maLoaiThuoc).ToList();

                var ws1 = wb.Worksheet("DMSP");
                int i = 2, j = 1;
                var soTT = 1;
                foreach (var item in record)
                {
                    ws1.Cell(i, j).Value = soTT;

                    ws1.Cell(i, j).Value = item.maThuoc;
                    ws1.Cell(i, j + 1).Value = item.tenThuoc;
                    ws1.Cell(i, j + 2).Value = item.maSanPham;
                    ws1.Cell(i, j + 3).Value = item.tenSanPham;
                    ws1.Cell(i, j + 4).Value = item.soThung;
                    ws1.Cell(i, j + 5).Value = item.donGia;
                    ws1.Cell(i, j + 6).Value = item.diem;
                    i++;
                    soTT++;
                }

                string filename = "DanhMucSanPham.xlsx";
                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=" + filename);
                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                }
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.ToString();
                return View("Failed");
            }
        }

        public List<tbl_FileDinhKem> fileDinhKems { get; set; }
    }
}
