CREATE DEFINER=`gc`@`%` PROCEDURE `LayThongTinCSDL`()
BEGIN
/*
canh_bao_do
0   kg validate
1   >0 canh bao
-1  =0 canh bao
 */
	select 'Tổng số câu hỏi' as ten, count(1) as so_luong, 0 as canh_bao_do from cau_hoi where 1=1
	union all
	select concat(' - Câu hỏi ', dm.ma) as title, count(ch.id), -1 from cau_hoi ch 
		inner join (select id, ma, ten from danh_muc where ma_dinh_danh='danh-muc-loai-cau-hoi') dm 
		on ch.loai_id=dm.id
		group by dm.ma
	union all
	select ' ==> Câu hỏi lỗi GRP không có câu hỏi con' as title, count(1), 1 from cau_hoi ch where (select count(1) from cau_hoi where parent_id = ch.id) = 0
		and loai_id IN (select id from danh_muc where ma='GRP' and ma_dinh_danh='danh-muc-loai-cau-hoi')
        and parent_id is null
	union all
	select ' ==> Câu hỏi lỗi không có đáp án' as title, count(1), 1 from cau_hoi where id not in (select cau_hoi_id from dap_an)
	union all
	select ' ==> Câu hỏi lỗi không có đáp án đúng' as title, count(1), 1 from cau_hoi where id not in (select cau_hoi_id from dap_an where is_correct=1)
	union all
	select ' ==> Câu hỏi lỗi (có trường quan trọng NULL)' as title, count(1), 1 from cau_hoi where chu_de_id is null or loai_id is null or muc_do_id is null or thanh_phan_nang_luc_id is null
		 or ma = '' or noi_dung = '' or ma is null or noi_dung is null or thu_tu is null or is_dao_dap_an is null or qd_dao_lenh_hoi is null
	union all
	select 'Tổng số câu đáp án' as title, count(1), -1 from dap_an where 1=1
	union all
	select ' ==> Đáp án lỗi (nội dung hoặc thứ tự NULL)' as title, count(1), 1 from dap_an where dap_an_text is null or dap_an_text = '' or thu_tu_dap_an is null
	union all
    
    
	select 'Thống kê môn học - chủ đề - câu hỏi' as title, count(1), -1 from dap_an where 1=1
	union all
    select * from (select concat(' - ', mon.ten, ' - ', cd.ten), count(ch.id) so_cau_hoi, 0 from (select id,ten from danh_muc where ma_dinh_danh='danh-muc-mon-thi') mon 
		inner join chu_de cd on cd.mon_hoc_id = mon.id
		inner join cau_hoi ch on ch.chu_de_id = cd.id
		group by mon.ten, cd.ten

		order by mon.ten, cd.ten) t
	;
END