-- One PostgreSQL server for local development, but one database AND one login per service.
-- A service can only connect to its own database: data ownership is enforced, not just agreed on.
CREATE ROLE shipping_svc      LOGIN PASSWORD 'shipping_pwd';
CREATE ROLE fleet_svc         LOGIN PASSWORD 'fleet_pwd';
CREATE ROLE dispatch_svc      LOGIN PASSWORD 'dispatch_pwd';
CREATE ROLE billing_svc       LOGIN PASSWORD 'billing_pwd';
CREATE ROLE notifications_svc LOGIN PASSWORD 'notifications_pwd';
CREATE ROLE analytics_svc     LOGIN PASSWORD 'analytics_pwd';

CREATE DATABASE shipping      OWNER shipping_svc;
CREATE DATABASE fleet         OWNER fleet_svc;
CREATE DATABASE dispatch      OWNER dispatch_svc;
CREATE DATABASE billing       OWNER billing_svc;
CREATE DATABASE notifications OWNER notifications_svc;
CREATE DATABASE analytics     OWNER analytics_svc;

REVOKE ALL ON DATABASE shipping      FROM PUBLIC;
REVOKE ALL ON DATABASE fleet         FROM PUBLIC;
REVOKE ALL ON DATABASE dispatch      FROM PUBLIC;
REVOKE ALL ON DATABASE billing       FROM PUBLIC;
REVOKE ALL ON DATABASE notifications FROM PUBLIC;
REVOKE ALL ON DATABASE analytics     FROM PUBLIC;
