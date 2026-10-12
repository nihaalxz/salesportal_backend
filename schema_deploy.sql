IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [notifications] (
        [id] nvarchar(36) NOT NULL,
        [type] nvarchar(255) NOT NULL,
        [notifiable_type] nvarchar(255) NOT NULL,
        [notifiable_id] bigint NOT NULL,
        [data] nvarchar(max) NOT NULL,
        [read_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_notifications] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [users] (
        [id] bigint NOT NULL IDENTITY,
        [name] nvarchar(255) NOT NULL,
        [email] nvarchar(255) NOT NULL,
        [avatar] nvarchar(255) NULL,
        [user_type] nvarchar(255) NOT NULL,
        [is_active] bit NOT NULL,
        [password] nvarchar(255) NOT NULL,
        [remember_token] nvarchar(100) NULL,
        [email_verified_at] datetime2 NULL,
        [last_login_at] datetime2 NULL,
        [last_login_ip] nvarchar(255) NULL,
        [profit_target] decimal(15,3) NOT NULL,
        [monthly_target] decimal(15,3) NOT NULL,
        [profit_generated] decimal(15,3) NOT NULL,
        [incentive] decimal(15,3) NOT NULL,
        [basic_salary] decimal(15,3) NOT NULL,
        [housing_allowance] decimal(15,3) NOT NULL,
        [transport_allowance] decimal(15,3) NOT NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [cash_in_hands] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [amount] decimal(15,3) NOT NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_cash_in_hands] PRIMARY KEY ([id]),
        CONSTRAINT [FK_cash_in_hands_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [deposits] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [amount] decimal(15,3) NOT NULL,
        [status] nvarchar(255) NOT NULL,
        [type] nvarchar(255) NOT NULL,
        [destination_id] bigint NOT NULL,
        [narration] nvarchar(255) NULL,
        [date] datetime2 NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [api_transaction_id] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_deposits] PRIMARY KEY ([id]),
        CONSTRAINT [FK_deposits_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [disputes] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [disputable_id] bigint NOT NULL,
        [disputable_type] nvarchar(255) NOT NULL,
        [subject] nvarchar(255) NOT NULL,
        [description] nvarchar(max) NOT NULL,
        [status] nvarchar(255) NOT NULL,
        [resolution_note] nvarchar(max) NULL,
        [refund_amount] decimal(15,3) NULL,
        [refund_status] nvarchar(255) NULL,
        [refund_date] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_disputes] PRIMARY KEY ([id]),
        CONSTRAINT [FK_disputes_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [heads] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NULL,
        [user_id] nvarchar(255) NULL,
        [type] nvarchar(255) NOT NULL,
        [first_name] nvarchar(255) NULL,
        [last_name] nvarchar(255) NULL,
        [mobile_no] nvarchar(20) NOT NULL,
        [email] nvarchar(255) NOT NULL,
        [nationality] nvarchar(255) NOT NULL,
        [balance] decimal(15,3) NOT NULL,
        [balance_amount] decimal(15,3) NOT NULL,
        [wallet] decimal(15,3) NOT NULL,
        [address] nvarchar(max) NOT NULL,
        [status] nvarchar(20) NOT NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_heads] PRIMARY KEY ([id]),
        CONSTRAINT [FK_heads_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [leads] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NOT NULL,
        [name] nvarchar(255) NOT NULL,
        [country_code] nvarchar(255) NOT NULL,
        [mobile_no] nvarchar(255) NOT NULL,
        [services] nvarchar(max) NULL,
        [lead_source] nvarchar(255) NULL,
        [description] nvarchar(max) NULL,
        [status] nvarchar(255) NOT NULL,
        [reminder_date] datetime2 NULL,
        [reminder_active] bit NOT NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_leads] PRIMARY KEY ([id]),
        CONSTRAINT [FK_leads_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [staff_monthly_records] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [month] nvarchar(255) NOT NULL,
        [year] int NOT NULL,
        [profit_generated] decimal(15,3) NOT NULL,
        [incentive] decimal(15,3) NOT NULL,
        [profit_target] decimal(15,3) NOT NULL,
        [monthly_target] decimal(15,3) NOT NULL,
        [target_achieved] bit NOT NULL,
        [basic_salary] decimal(15,3) NOT NULL,
        [housing_allowance] decimal(15,3) NOT NULL,
        [transport_allowance] decimal(15,3) NOT NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_staff_monthly_records] PRIMARY KEY ([id]),
        CONSTRAINT [FK_staff_monthly_records_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [flight_bookings] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [head_id] bigint NULL,
        [client_type] nvarchar(255) NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [care_of] nvarchar(255) NULL,
        [supplier] nvarchar(255) NULL,
        [from_location] nvarchar(255) NOT NULL,
        [to_location] nvarchar(255) NOT NULL,
        [payment_method] nvarchar(255) NOT NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_flight_bookings] PRIMARY KEY ([id]),
        CONSTRAINT [FK_flight_bookings_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_flight_bookings_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [head_ledgers] (
        [id] bigint NOT NULL IDENTITY,
        [head_id] bigint NOT NULL,
        [service_type] nvarchar(255) NOT NULL,
        [service_id] bigint NOT NULL,
        [debit] decimal(15,3) NOT NULL,
        [credit] decimal(15,3) NOT NULL,
        [description] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_head_ledgers] PRIMARY KEY ([id]),
        CONSTRAINT [FK_head_ledgers_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [holiday_packages] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NOT NULL,
        [head_id] bigint NULL,
        [client_type] nvarchar(255) NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [booking_id] nvarchar(255) NOT NULL,
        [holiday_app_id] nvarchar(255) NULL,
        [prefix] nvarchar(255) NOT NULL,
        [passenger_name] nvarchar(255) NOT NULL,
        [supplier] nvarchar(255) NOT NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [country] nvarchar(255) NOT NULL,
        [days] int NOT NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [payment_mode] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_holiday_packages] PRIMARY KEY ([id]),
        CONSTRAINT [FK_holiday_packages_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_holiday_packages_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [hotel_bookings] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [head_id] bigint NULL,
        [client_type] nvarchar(255) NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [is_inhouse] bit NOT NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [prefix] nvarchar(255) NULL,
        [guest_name] nvarchar(255) NOT NULL,
        [care_of] nvarchar(255) NULL,
        [supplier] nvarchar(255) NULL,
        [country] nvarchar(255) NOT NULL,
        [days] int NOT NULL,
        [check_in_date] datetime2 NOT NULL,
        [payment_method] nvarchar(255) NOT NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [order_status] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [payment_reference] nvarchar(255) NULL,
        [bank_id] bigint NULL,
        [bank_name] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_hotel_bookings] PRIMARY KEY ([id]),
        CONSTRAINT [FK_hotel_bookings_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_hotel_bookings_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [other_services] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NOT NULL,
        [head_id] bigint NULL,
        [client_type] nvarchar(255) NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [prefix] nvarchar(255) NULL,
        [name] nvarchar(255) NULL,
        [care_of] nvarchar(255) NULL,
        [supplier] nvarchar(255) NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [service_name] nvarchar(255) NULL,
        [narration] nvarchar(max) NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [payment_method] nvarchar(255) NOT NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_other_services] PRIMARY KEY ([id]),
        CONSTRAINT [FK_other_services_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_other_services_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [receipts] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NULL,
        [receipt_id] nvarchar(255) NOT NULL,
        [head_id] bigint NULL,
        [booking_id] nvarchar(255) NULL,
        [narration] nvarchar(255) NOT NULL,
        [service] nvarchar(255) NOT NULL,
        [amount] decimal(15,3) NOT NULL,
        [payment_mode] nvarchar(20) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [status] nvarchar(20) NOT NULL,
        [payment_reference] nvarchar(255) NULL,
        [bank_id] bigint NULL,
        [bank_name] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_receipts] PRIMARY KEY ([id]),
        CONSTRAINT [FK_receipts_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_receipts_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [sales_visa_applications] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [country_id] int NOT NULL,
        [visa_pack_id] int NOT NULL,
        [visa_pack_name] nvarchar(255) NULL,
        [client_type] nvarchar(255) NOT NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [head_id] bigint NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [visa_document] nvarchar(255) NULL,
        [approval_note] nvarchar(max) NULL,
        [rejection_document] nvarchar(255) NULL,
        [rejection_note] nvarchar(max) NULL,
        [refund_note] nvarchar(max) NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [rejection_fee] decimal(15,3) NOT NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_sales_visa_applications] PRIMARY KEY ([id]),
        CONSTRAINT [FK_sales_visa_applications_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_sales_visa_applications_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [transportations] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NOT NULL,
        [head_id] bigint NULL,
        [client_type] nvarchar(255) NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [prefix] nvarchar(255) NULL,
        [name] nvarchar(255) NULL,
        [care_of] nvarchar(255) NULL,
        [supplier] nvarchar(255) NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [pickup] nvarchar(255) NULL,
        [drop] nvarchar(255) NULL,
        [drop_date] datetime2 NULL,
        [drop_time] time NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [payment_method] nvarchar(255) NOT NULL,
        [vehicle] nvarchar(255) NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [is_edited] bit NOT NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_transportations] PRIMARY KEY ([id]),
        CONSTRAINT [FK_transportations_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_transportations_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [vfs_appointments] (
        [id] bigint NOT NULL IDENTITY,
        [created_by] bigint NOT NULL,
        [head_id] bigint NULL,
        [client_type] nvarchar(255) NULL,
        [agent] nvarchar(255) NULL,
        [agent_id] bigint NULL,
        [vfs_id] nvarchar(255) NOT NULL,
        [vfs_app_id] nvarchar(255) NULL,
        [prefix] nvarchar(255) NOT NULL,
        [name] nvarchar(255) NOT NULL,
        [supplier] nvarchar(255) NOT NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [country] nvarchar(255) NOT NULL,
        [visa_type] nvarchar(255) NOT NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [payment_mode] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [processing_status] nvarchar(255) NOT NULL,
        [appointment_date] datetime2 NULL,
        [appointment_time] time NULL,
        [vfs_receipt] nvarchar(255) NULL,
        [passport_copy] nvarchar(255) NULL,
        [deleted_at] datetime2 NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_vfs_appointments] PRIMARY KEY ([id]),
        CONSTRAINT [FK_vfs_appointments_heads_head_id] FOREIGN KEY ([head_id]) REFERENCES [heads] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_vfs_appointments_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [flight_passengers] (
        [id] bigint NOT NULL IDENTITY,
        [flight_booking_id] bigint NOT NULL,
        [prefix] nvarchar(255) NULL,
        [passenger_name] nvarchar(255) NOT NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [supplier_cost] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [is_inhouse] bit NOT NULL,
        [supplier_id] bigint NULL,
        [supplier_name] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_flight_passengers] PRIMARY KEY ([id]),
        CONSTRAINT [FK_flight_passengers_flight_bookings_flight_booking_id] FOREIGN KEY ([flight_booking_id]) REFERENCES [flight_bookings] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [holiday_package_documents] (
        [id] bigint NOT NULL IDENTITY,
        [holiday_package_id] bigint NOT NULL,
        [document_path] nvarchar(255) NOT NULL,
        [document_type] nvarchar(255) NOT NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_holiday_package_documents] PRIMARY KEY ([id]),
        CONSTRAINT [FK_holiday_package_documents_holiday_packages_holiday_package_id] FOREIGN KEY ([holiday_package_id]) REFERENCES [holiday_packages] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [holiday_package_govt_fees] (
        [id] bigint NOT NULL IDENTITY,
        [holiday_package_id] bigint NOT NULL,
        [amount] decimal(15,3) NOT NULL,
        [payment_reference] nvarchar(255) NULL,
        [status] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_holiday_package_govt_fees] PRIMARY KEY ([id]),
        CONSTRAINT [FK_holiday_package_govt_fees_holiday_packages_holiday_package_id] FOREIGN KEY ([holiday_package_id]) REFERENCES [holiday_packages] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [sales_visa_applicants] (
        [id] bigint NOT NULL IDENTITY,
        [sales_visa_application_id] bigint NOT NULL,
        [visa_app_id] nvarchar(255) NULL,
        [prefix] nvarchar(255) NULL,
        [f_name] nvarchar(255) NOT NULL,
        [l_name] nvarchar(255) NOT NULL,
        [dob] datetime2 NULL,
        [nationality] nvarchar(255) NOT NULL,
        [care_of] nvarchar(255) NULL,
        [contact_no] nvarchar(255) NOT NULL,
        [email_id] nvarchar(255) NOT NULL,
        [passport_no] nvarchar(255) NOT NULL,
        [iqama_hotel_no] nvarchar(255) NULL,
        [payable_amount] decimal(15,3) NOT NULL,
        [unpaid_amount] decimal(15,3) NULL,
        [bank_doc] nvarchar(255) NULL,
        [passport_front] nvarchar(255) NULL,
        [passport_back] nvarchar(255) NULL,
        [payment_reference] nvarchar(255) NULL,
        [payment_method] nvarchar(255) NULL,
        [payment_status] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [processed_by] bigint NULL,
        [payment_confirmed_by] bigint NULL,
        [supplier_cost] decimal(15,3) NOT NULL,
        [expected_supplier_cost] decimal(15,3) NOT NULL,
        [profit] decimal(15,3) NOT NULL,
        [is_inhouse] bit NOT NULL,
        [bank_id] bigint NULL,
        [bank_name] nvarchar(255) NULL,
        [govt_fee_amount] decimal(15,3) NULL,
        [govt_fee_reference] nvarchar(255) NULL,
        [govt_fee_status] nvarchar(255) NOT NULL,
        [govt_fee_payment_proof] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_sales_visa_applicants] PRIMARY KEY ([id]),
        CONSTRAINT [FK_sales_visa_applicants_sales_visa_applications_sales_visa_application_id] FOREIGN KEY ([sales_visa_application_id]) REFERENCES [sales_visa_applications] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [vfs_appointment_govt_fees] (
        [id] bigint NOT NULL IDENTITY,
        [vfs_appointment_id] bigint NOT NULL,
        [amount] decimal(15,3) NOT NULL,
        [payment_reference] nvarchar(255) NULL,
        [status] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_vfs_appointment_govt_fees] PRIMARY KEY ([id]),
        CONSTRAINT [FK_vfs_appointment_govt_fees_vfs_appointments_vfs_appointment_id] FOREIGN KEY ([vfs_appointment_id]) REFERENCES [vfs_appointments] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [sales_visa_govt_fees] (
        [id] bigint NOT NULL IDENTITY,
        [sales_visa_applicant_id] bigint NOT NULL,
        [amount] decimal(15,3) NOT NULL,
        [description] nvarchar(255) NULL,
        [payment_reference] nvarchar(255) NULL,
        [status] nvarchar(255) NOT NULL,
        [payment_proof] nvarchar(255) NULL,
        [created_at] datetime2 NULL,
        [updated_at] datetime2 NULL,
        CONSTRAINT [PK_sales_visa_govt_fees] PRIMARY KEY ([id]),
        CONSTRAINT [FK_sales_visa_govt_fees_sales_visa_applicants_sales_visa_applicant_id] FOREIGN KEY ([sales_visa_applicant_id]) REFERENCES [sales_visa_applicants] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_cash_in_hands_user_id] ON [cash_in_hands] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_deposits_user_id] ON [deposits] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_disputes_user_id] ON [disputes] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_flight_bookings_head_id] ON [flight_bookings] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_flight_bookings_user_id] ON [flight_bookings] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_flight_passengers_flight_booking_id] ON [flight_passengers] ([flight_booking_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_head_ledgers_head_id] ON [head_ledgers] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [unique_service_ledger_entry] ON [head_ledgers] ([service_type], [service_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_heads_created_by] ON [heads] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_heads_email] ON [heads] ([email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_heads_user_id] ON [heads] ([user_id]) WHERE [user_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_holiday_package_documents_holiday_package_id] ON [holiday_package_documents] ([holiday_package_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_holiday_package_govt_fees_holiday_package_id] ON [holiday_package_govt_fees] ([holiday_package_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_holiday_packages_booking_id] ON [holiday_packages] ([booking_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_holiday_packages_created_by] ON [holiday_packages] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_holiday_packages_head_id] ON [holiday_packages] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_hotel_bookings_head_id] ON [hotel_bookings] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_hotel_bookings_user_id] ON [hotel_bookings] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_leads_created_by] ON [leads] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_other_services_created_by] ON [other_services] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_other_services_head_id] ON [other_services] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_receipts_created_by] ON [receipts] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_receipts_head_id] ON [receipts] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_receipts_receipt_id] ON [receipts] ([receipt_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_sales_visa_applicants_sales_visa_application_id] ON [sales_visa_applicants] ([sales_visa_application_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_sales_visa_applications_head_id] ON [sales_visa_applications] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_sales_visa_applications_user_id] ON [sales_visa_applications] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_sales_visa_govt_fees_sales_visa_applicant_id] ON [sales_visa_govt_fees] ([sales_visa_applicant_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_staff_monthly_records_user_id] ON [staff_monthly_records] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_transportations_created_by] ON [transportations] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_transportations_head_id] ON [transportations] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_email] ON [users] ([email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_vfs_appointment_govt_fees_vfs_appointment_id] ON [vfs_appointment_govt_fees] ([vfs_appointment_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_vfs_appointments_created_by] ON [vfs_appointments] ([created_by]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_vfs_appointments_head_id] ON [vfs_appointments] ([head_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_vfs_appointments_vfs_id] ON [vfs_appointments] ([vfs_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710132320_InitialSqlServerSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710132320_InitialSqlServerSchema', N'9.0.0');
END;

COMMIT;
GO

