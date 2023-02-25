USE [erp]
GO
/****** Object:  StoredProcedure [dbo].[sp_QLTT_DanhMucSanPham_Index]    Script Date: 2023/02/22 20:19:30 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







ALTER PROCEDURE [dbo].[sp_QLTT_DanhMucSanPham_Index]
	@qSearch nvarchar(max)=N'TR45S4',
	@tuNgay datetime =null,
	@denNgay datetime=null,
	@page int = 1,
	@rowNumber int = 50,
	@maLoaiThuoc nvarchar(200) = null
AS
BEGIN		
	
	declare @KetQua table (
	maSanPham nvarchar(250),
tenSanPham nvarchar(250),
donGia float,
soThung float,
diem float,
ngayLap	datetime,
nguoiLap nvarchar(250),
maThuoc nvarchar(150),
tenThuoc nvarchar(150),
maNhomLeft nvarchar(150))
		
		insert into @KetQua 
		SELECT  distinct maSanPham,
tenSanPham,
donGia,
soThung,
p.diemTrenThung,
p.ngayLap,
nv.Ho+' ' + nv.Ten,
dmt.maThuoc,
dmt.tenThuoc,
p.maNhomLeft




		FROM  [dbo].[tbl_QLTT_DanhMucSanPham] p
			
			LEFT JOIN Sys_NhanVien nv on nv.Manv = p.nguoiLap
			LEFT JOIN tbl_QLTT_DanhMucThuoc dmt on p.maThuoc = dmt.maThuoc
		WHERE  
			(@tuNgay is null or @tuNgay='' or @tuNgay <= p.ngayLap)
			and (@denNgay is null  or @denNgay='' or @denNgay>=p.ngayLap)		
			and (@maLoaiThuoc is null or @maLoaiThuoc = '' or @maLoaiThuoc = dmt.maThuoc)and (
			@qSearch is null or @qSearch='' or  p.donGia  like '%' + @qSearch +'%'
			or  p.maSanPham  like '%' + @qSearch +'%'
			or  p.tenSanPham  like '%' + @qSearch +'%'
			or  p.maNhomLeft  like '%' + @qSearch +'%'
			)
				declare  @tongSoDong int= ( select COUNT(1) from @KetQua)	
	SELECT *,@tongSoDong tongSoDong FROM (
             SELECT ROW_NUMBER() OVER(ORDER BY (SELECT 1)) AS NUMBER,*
	from @KetQua 
		 ) AS TBL
	where NUMBER BETWEEN (@page-1)*@rowNumber + 1  AND @rowNumber*@page			
	order by maSanPham desc
	
	

end







