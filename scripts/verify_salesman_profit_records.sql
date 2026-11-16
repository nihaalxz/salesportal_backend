-- ==============================================================================
-- SCRIPT: verify_salesman_profit_records.sql
-- PURPOSE: Read-only verification script checking:
--          1. Bookings count vs Profit Records count per Service Type
--          2. Bookings count vs Profit Records count per Salesman
--          3. Expected Profit from booking tables vs Profit in records table
--          4. Discrepancy report highlighting any gaps
-- ==============================================================================

SET NOCOUNT ON;

PRINT '==============================================================================';
PRINT '1. RECONCILIATION SUMMARY: BOOKINGS VS PROFIT RECORDS BY SERVICE TYPE';
PRINT '==============================================================================';

WITH AllBookings AS (
    SELECT 'Flight' AS ServiceType, id, user_id AS SalesmanId, payable_amount, expected_supplier_cost, profit, processing_status FROM flight_bookings WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Hotel', id, user_id, payable_amount, expected_supplier_cost, profit, processing_status FROM hotel_bookings WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Visa', id, user_id, payable_amount, expected_supplier_cost, profit, processing_status FROM sales_visa_applications WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Vfs', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM vfs_appointments WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Holiday', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM holiday_packages WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Transportation', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM transportations WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Other', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM other_services WHERE deleted_at IS NULL
),
BookingSummary AS (
    SELECT
        ServiceType,
        COUNT(*) AS TotalBookings,
        SUM(ISNULL(payable_amount, 0)) AS TotalPayable,
        SUM(
            CASE
                WHEN LOWER(ISNULL(processing_status, '')) IN ('cancelled', 'voided') THEN 0
                WHEN LOWER(ISNULL(processing_status, '')) = 'rejected' THEN 0
                WHEN ISNULL(profit, 0) > 0 THEN profit
                WHEN (ISNULL(payable_amount, 0) - ISNULL(expected_supplier_cost, 0)) > 0 
                    THEN (payable_amount - expected_supplier_cost)
                ELSE 0
            END
        ) AS TotalExpectedProfit
    FROM AllBookings
    GROUP BY ServiceType
),
RecordSummary AS (
    SELECT
        booking_type AS ServiceType,
        COUNT(*) AS TotalRecords,
        SUM(ISNULL(payable_amount, 0)) AS RecordPayable,
        SUM(ISNULL(profit_amount, 0)) AS RecordProfit,
        SUM(CASE WHEN is_final = 1 THEN ISNULL(profit_amount, 0) ELSE 0 END) AS FinalProfit
    FROM salesman_profit_records
    GROUP BY booking_type
)
SELECT
    COALESCE(b.ServiceType, r.ServiceType) AS ServiceType,
    ISNULL(b.TotalBookings, 0) AS BookingsCount,
    ISNULL(r.TotalRecords, 0) AS RecordsCount,
    (ISNULL(b.TotalBookings, 0) - ISNULL(r.TotalRecords, 0)) AS MissingRecords,
    ISNULL(b.TotalPayable, 0) AS BookingsPayable,
    ISNULL(r.RecordPayable, 0) AS RecordsPayable,
    ISNULL(b.TotalExpectedProfit, 0) AS BookingsExpectedProfit,
    ISNULL(r.RecordProfit, 0) AS RecordsExpectedProfit,
    (ISNULL(b.TotalExpectedProfit, 0) - ISNULL(r.RecordProfit, 0)) AS ProfitDifference,
    ISNULL(r.FinalProfit, 0) AS RealizedFinalProfit
FROM BookingSummary b
FULL OUTER JOIN RecordSummary r ON b.ServiceType = r.ServiceType
ORDER BY ServiceType;


PRINT '';
PRINT '==============================================================================';
PRINT '2. RECONCILIATION SUMMARY: BOOKINGS VS PROFIT RECORDS BY SALESMAN';
PRINT '==============================================================================';

WITH AllBookings AS (
    SELECT 'Flight' AS ServiceType, id, user_id AS SalesmanId, payable_amount, expected_supplier_cost, profit, processing_status FROM flight_bookings WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Hotel', id, user_id, payable_amount, expected_supplier_cost, profit, processing_status FROM hotel_bookings WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Visa', id, user_id, payable_amount, expected_supplier_cost, profit, processing_status FROM sales_visa_applications WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Vfs', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM vfs_appointments WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Holiday', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM holiday_packages WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Transportation', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM transportations WHERE deleted_at IS NULL
    UNION ALL
    SELECT 'Other', id, created_by, payable_amount, expected_supplier_cost, profit, processing_status FROM other_services WHERE deleted_at IS NULL
),
BookingBySalesman AS (
    SELECT
        SalesmanId,
        COUNT(*) AS TotalBookings,
        SUM(ISNULL(payable_amount, 0)) AS TotalPayable,
        SUM(
            CASE
                WHEN LOWER(ISNULL(processing_status, '')) IN ('cancelled', 'voided') THEN 0
                WHEN LOWER(ISNULL(processing_status, '')) = 'rejected' THEN 0
                WHEN ISNULL(profit, 0) > 0 THEN profit
                WHEN (ISNULL(payable_amount, 0) - ISNULL(expected_supplier_cost, 0)) > 0 
                    THEN (payable_amount - expected_supplier_cost)
                ELSE 0
            END
        ) AS ExpectedProfit
    FROM AllBookings
    GROUP BY SalesmanId
),
RecordBySalesman AS (
    SELECT
        salesman_id AS SalesmanId,
        COUNT(*) AS TotalRecords,
        SUM(ISNULL(payable_amount, 0)) AS RecordPayable,
        SUM(ISNULL(profit_amount, 0)) AS RecordProfit,
        SUM(CASE WHEN is_final = 1 THEN ISNULL(profit_amount, 0) ELSE 0 END) AS FinalProfit
    FROM salesman_profit_records
    GROUP BY salesman_id
)
SELECT
    COALESCE(u.id, b.SalesmanId, r.SalesmanId) AS SalesmanId,
    COALESCE(u.name, 'Unknown / Unassigned') AS SalesmanName,
    ISNULL(b.TotalBookings, 0) AS BookingsCount,
    ISNULL(r.TotalRecords, 0) AS RecordsCount,
    (ISNULL(b.TotalBookings, 0) - ISNULL(r.TotalRecords, 0)) AS MissingRecords,
    ISNULL(b.TotalPayable, 0) AS BookingsPayable,
    ISNULL(b.ExpectedProfit, 0) AS BookingsExpectedProfit,
    ISNULL(r.RecordProfit, 0) AS RecordsExpectedProfit,
    (ISNULL(b.ExpectedProfit, 0) - ISNULL(r.RecordProfit, 0)) AS ProfitDifference,
    ISNULL(r.FinalProfit, 0) AS RealizedFinalProfit
FROM BookingBySalesman b
FULL OUTER JOIN RecordBySalesman r ON b.SalesmanId = r.SalesmanId
LEFT JOIN users u ON u.id = COALESCE(b.SalesmanId, r.SalesmanId)
ORDER BY SalesmanId;


PRINT '';
PRINT '==============================================================================';
PRINT '3. ORPHANED OR UNMATCHED PROFIT RECORDS (Records without matching booking)';
PRINT '==============================================================================';

SELECT
    spr.id AS ProfitRecordId,
    spr.salesman_id AS SalesmanId,
    spr.booking_type AS BookingType,
    spr.booking_id AS BookingId,
    spr.document_number AS DocumentNumber,
    spr.payable_amount AS PayableAmount,
    spr.profit_amount AS ProfitAmount,
    spr.is_final AS IsFinal
FROM salesman_profit_records spr
WHERE NOT EXISTS (
    SELECT 1 FROM flight_bookings fb WHERE spr.booking_type = 'Flight' AND fb.id = spr.booking_id AND fb.deleted_at IS NULL
    UNION ALL
    SELECT 1 FROM hotel_bookings hb WHERE spr.booking_type = 'Hotel' AND hb.id = spr.booking_id AND hb.deleted_at IS NULL
    UNION ALL
    SELECT 1 FROM sales_visa_applications vb WHERE spr.booking_type = 'Visa' AND vb.id = spr.booking_id AND vb.deleted_at IS NULL
    UNION ALL
    SELECT 1 FROM vfs_appointments vfb WHERE spr.booking_type = 'Vfs' AND vfb.id = spr.booking_id AND vfb.deleted_at IS NULL
    UNION ALL
    SELECT 1 FROM holiday_packages hpb WHERE spr.booking_type = 'Holiday' AND hpb.id = spr.booking_id AND hpb.deleted_at IS NULL
    UNION ALL
    SELECT 1 FROM transportations tb WHERE spr.booking_type = 'Transportation' AND tb.id = spr.booking_id AND tb.deleted_at IS NULL
    UNION ALL
    SELECT 1 FROM other_services ob WHERE spr.booking_type = 'Other' AND ob.id = spr.booking_id AND ob.deleted_at IS NULL
);

PRINT 'Verification complete.';

