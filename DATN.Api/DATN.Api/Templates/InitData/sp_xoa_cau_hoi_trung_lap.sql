CREATE DEFINER=`gc`@`%` PROCEDURE `xoa_cau_hoi_trung_lap`()
BEGIN
    -- Bắt đầu transaction để đảm bảo tính toàn vẹn dữ liệu
    START TRANSACTION;

    -- Tạo bảng tạm chứa các cặp câu hỏi trùng lặp
    DROP TEMPORARY TABLE IF EXISTS temp_duplicates;
    CREATE TEMPORARY TABLE temp_duplicates AS
    SELECT 
        c1.id AS id_can_xoa,
        c2.id AS id_giu_lai
    FROM cau_hoi c1
    INNER JOIN cau_hoi c2 ON c1.id < c2.id -- Giữ lại câu hỏi có ID lớn hơn
    WHERE 
        REGEXP_REPLACE(c1.noi_dung, '<img[^>]*>', '') = REGEXP_REPLACE(c2.noi_dung, '<img[^>]*>', '')
        AND REGEXP_REPLACE(COALESCE(c1.dap_an_text, ''), '<img[^>]*>', '') = REGEXP_REPLACE(COALESCE(c2.dap_an_text, ''), '<img[^>]*>', '')
        AND LENGTH(c1.noi_dung) > 10
        AND LENGTH(c2.noi_dung) > 10;

    -- Cập nhật các câu hỏi con trỏ về câu hỏi cha được giữ lại
    UPDATE cau_hoi c
    INNER JOIN temp_duplicates td ON c.parent_id = td.id_can_xoa
    SET c.parent_id = td.id_giu_lai;

    -- Xóa các đáp án của các câu hỏi bị xóa
    DELETE FROM dap_an
    WHERE cau_hoi_id IN (
        SELECT id_can_xoa FROM temp_duplicates
    );

    -- Xoá trước các câu hỏi con (parent_id IS NOT NULL)
    DELETE c
    FROM cau_hoi c
    INNER JOIN temp_duplicates td ON c.id = td.id_can_xoa
    WHERE c.parent_id IS NOT NULL;

    -- Sau đó xoá các câu hỏi cha (parent_id IS NULL)
    DELETE c
    FROM cau_hoi c
    INNER JOIN temp_duplicates td ON c.id = td.id_can_xoa
    WHERE c.parent_id IS NULL;

    -- Xoá bảng tạm
    DROP TEMPORARY TABLE temp_duplicates;

    -- Hoàn tất transaction
    COMMIT;
END