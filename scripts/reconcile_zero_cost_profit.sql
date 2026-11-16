-- ==============================================================================
-- SCRIPT: reconcile_zero_cost_profit.sql
-- PURPOSE: Production Database Reconciliation (db59312)
--          Recognize full sale amount as profit when approved/supplier cost is 0.
-- ==============================================================================

BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Fix profit_amount on salesman_profit_records where supplier cost is 0
    UPDATE salesman_profit_records
    SET profit_amount = payable_amount - final_supplier_cost,
        updated_at = GETUTCDATE()
    WHERE (final_supplier_cost = 0 OR profit_type = 'Inhouse')
      AND profit_type != 'Rejection'
      AND profit_amount = 0
      AND payable_amount > 0;
    PRINT 'Updated salesman_profit_records: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 2. Sync profit on booking entities
    -- Visas (including TXSVISA416750 / Visa 368)
    UPDATE sva
    SET sva.profit = spr.profit_amount,
        sva.updated_at = GETUTCDATE()
    FROM sales_visa_applications sva
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'Visa' AND spr.booking_id = sva.id
    WHERE sva.profit != spr.profit_amount;
    PRINT 'Updated sales_visa_applications.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Flights
    UPDATE fb
    SET fb.profit = spr.profit_amount,
        fb.updated_at = GETUTCDATE()
    FROM flight_bookings fb
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'Flight' AND spr.booking_id = fb.id
    WHERE fb.profit != spr.profit_amount;
    PRINT 'Updated flight_bookings.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Hotels
    UPDATE hb
    SET hb.profit = spr.profit_amount,
        hb.updated_at = GETUTCDATE()
    FROM hotel_bookings hb
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'Hotel' AND spr.booking_id = hb.id
    WHERE hb.profit != spr.profit_amount;
    PRINT 'Updated hotel_bookings.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- VFS
    UPDATE vfs
    SET vfs.profit = spr.profit_amount,
        vfs.updated_at = GETUTCDATE()
    FROM vfs_appointments vfs
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'VFS' AND spr.booking_id = vfs.id
    WHERE vfs.profit != spr.profit_amount;
    PRINT 'Updated vfs_appointments.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Holidays
    UPDATE hp
    SET hp.profit = spr.profit_amount,
        hp.updated_at = GETUTCDATE()
    FROM holiday_packages hp
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'Holiday' AND spr.booking_id = hp.id
    WHERE hp.profit != spr.profit_amount;
    PRINT 'Updated holiday_packages.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Transport
    UPDATE tr
    SET tr.profit = spr.profit_amount,
        tr.updated_at = GETUTCDATE()
    FROM transportations tr
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'Transport' AND spr.booking_id = tr.id
    WHERE tr.profit != spr.profit_amount;
    PRINT 'Updated transportations.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Other Services
    UPDATE os
    SET os.profit = spr.profit_amount,
        os.updated_at = GETUTCDATE()
    FROM other_services os
    INNER JOIN salesman_profit_records spr ON spr.booking_type = 'Other' AND spr.booking_id = os.id
    WHERE os.profit != spr.profit_amount;
    PRINT 'Updated other_services.profit: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 3. Refresh users.profit_generated for all salesmen
    UPDATE u
    SET u.profit_generated = ISNULL(totals.SumProfit, 0)
    FROM users u
    LEFT JOIN (
        SELECT salesman_id, SUM(profit_amount) AS SumProfit
        FROM salesman_profit_records
        WHERE is_final = 1
        GROUP BY salesman_id
    ) totals ON totals.salesman_id = u.id
    WHERE u.profit_generated != ISNULL(totals.SumProfit, 0);
    PRINT 'Updated users.profit_generated: ' + CAST(@@ROWCOUNT AS VARCHAR);

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: Database reconciliation completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

