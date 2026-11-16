-- ==============================================================================
-- SCRIPT: backfill_salesman_profit_records.sql
-- PURPOSE: Idempotent backfill and normalization of salesman_profit_records
--          for all 7 booking service types. Ensures Expected Profit agreement
--          with booking tables and Sales Entries without touching finalized records.
-- GENERATE-ONLY: Do not apply directly without approval.
-- ==============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting salesman_profit_records backfill...';

    -- ──────────────────────────────────────────────────────────────────────────
    -- 1. BACKFILL MISSING RECORDS FOR FLIGHT BOOKINGS
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.user_id AS salesman_id,
        'Flight' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 0
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        NULL AS rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM flight_bookings b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Flight' AND spr.booking_id = b.id
      );

    PRINT 'Flight records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 2. BACKFILL MISSING RECORDS FOR HOTEL BOOKINGS
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.user_id AS salesman_id,
        'Hotel' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 0
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        NULL AS rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM hotel_bookings b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Hotel' AND spr.booking_id = b.id
      );

    PRINT 'Hotel records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 3. BACKFILL MISSING RECORDS FOR VISA APPLICATIONS
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.user_id AS salesman_id,
        'Visa' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN ISNULL(b.rejection_fee, 0)
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        b.rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM sales_visa_applications b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Visa' AND spr.booking_id = b.id
      );

    PRINT 'Visa records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 4. BACKFILL MISSING RECORDS FOR VFS APPOINTMENTS
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.created_by AS salesman_id,
        'Vfs' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 0
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        NULL AS rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM vfs_appointments b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Vfs' AND spr.booking_id = b.id
      );

    PRINT 'VFS records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 5. BACKFILL MISSING RECORDS FOR HOLIDAY PACKAGES
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.created_by AS salesman_id,
        'Holiday' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 0
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        NULL AS rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM holiday_packages b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Holiday' AND spr.booking_id = b.id
      );

    PRINT 'Holiday records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 6. BACKFILL MISSING RECORDS FOR TRANSPORTATIONS
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.created_by AS salesman_id,
        'Transportation' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 0
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        NULL AS rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM transportations b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Transportation' AND spr.booking_id = b.id
      );

    PRINT 'Transportation records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 7. BACKFILL MISSING RECORDS FOR OTHER SERVICES
    -- ──────────────────────────────────────────────────────────────────────────
    INSERT INTO salesman_profit_records (
        salesman_id,
        booking_type,
        booking_id,
        document_number,
        payable_amount,
        final_supplier_cost,
        profit_amount,
        profit_type,
        supplier_cost_source,
        accounts_approved_total,
        rejection_fee,
        is_final,
        booking_created_at,
        finalized_at,
        last_reconciled_at,
        created_at,
        updated_at
    )
    SELECT
        b.created_by AS salesman_id,
        'Other' AS booking_type,
        b.id AS booking_id,
        b.document_number,
        ISNULL(b.payable_amount, 0) AS payable_amount,
        ISNULL(b.expected_supplier_cost, 0) AS final_supplier_cost,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) IN ('cancelled', 'voided') THEN 0
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 0
            WHEN ISNULL(b.profit, 0) > 0 THEN b.profit
            WHEN (ISNULL(b.payable_amount, 0) - ISNULL(b.expected_supplier_cost, 0)) > 0 
                THEN (b.payable_amount - b.expected_supplier_cost)
            ELSE 0
        END AS profit_amount,
        CASE
            WHEN LOWER(ISNULL(b.processing_status, '')) = 'rejected' THEN 'Rejection'
            WHEN LOWER(ISNULL(b.payment_method, '')) = 'inhouse' THEN 'Inhouse'
            ELSE 'Normal'
        END AS profit_type,
        'Estimated' AS supplier_cost_source,
        NULL AS accounts_approved_total,
        NULL AS rejection_fee,
        0 AS is_final,
        b.created_at AS booking_created_at,
        NULL AS finalized_at,
        NULL AS last_reconciled_at,
        GETUTCDATE() AS created_at,
        GETUTCDATE() AS updated_at
    FROM other_services b
    WHERE b.deleted_at IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM salesman_profit_records spr
          WHERE spr.booking_type = 'Other' AND spr.booking_id = b.id
      );

    PRINT 'Other Services records backfilled: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    -- ──────────────────────────────────────────────────────────────────────────
    -- 8. UPDATE NON-FINAL INHOUSE RECORDS WITH MISLEADING 0 PROFIT
    --    Only touches records where is_final = 0 and profit_type = 'Inhouse'.
    --    Never touches finalized records.
    -- ──────────────────────────────────────────────────────────────────────────
    UPDATE spr
    SET spr.final_supplier_cost = ISNULL(bk.ExpectedCost, spr.final_supplier_cost),
        spr.profit_amount = CASE
            WHEN bk.Profit > 0 THEN bk.Profit
            WHEN (spr.payable_amount - ISNULL(bk.ExpectedCost, spr.final_supplier_cost)) > 0 
                THEN (spr.payable_amount - ISNULL(bk.ExpectedCost, spr.final_supplier_cost))
            ELSE 0
        END,
        spr.updated_at = GETUTCDATE()
    FROM salesman_profit_records spr
    INNER JOIN (
        SELECT 'Flight' AS BookingType, id, expected_supplier_cost AS ExpectedCost, profit AS Profit FROM flight_bookings WHERE deleted_at IS NULL
        UNION ALL
        SELECT 'Hotel', id, expected_supplier_cost, profit FROM hotel_bookings WHERE deleted_at IS NULL
        UNION ALL
        SELECT 'Visa', id, expected_supplier_cost, profit FROM sales_visa_applications WHERE deleted_at IS NULL
        UNION ALL
        SELECT 'Vfs', id, expected_supplier_cost, profit FROM vfs_appointments WHERE deleted_at IS NULL
        UNION ALL
        SELECT 'Holiday', id, expected_supplier_cost, profit FROM holiday_packages WHERE deleted_at IS NULL
        UNION ALL
        SELECT 'Transportation', id, expected_supplier_cost, profit FROM transportations WHERE deleted_at IS NULL
        UNION ALL
        SELECT 'Other', id, expected_supplier_cost, profit FROM other_services WHERE deleted_at IS NULL
    ) bk ON spr.booking_type = bk.BookingType AND spr.booking_id = bk.id
    WHERE spr.is_final = 0
      AND spr.profit_type = 'Inhouse'
      AND spr.profit_amount = 0
      AND spr.payable_amount > 0;

    PRINT 'Inhouse non-final records updated with expected margin: ' + CAST(@@ROWCOUNT AS VARCHAR(20));

    COMMIT TRANSACTION;
    PRINT 'Backfill completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT 'Error during backfill: ' + @ErrMsg;
    THROW;
END CATCH;

