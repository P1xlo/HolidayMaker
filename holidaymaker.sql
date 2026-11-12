-- =====================================================================
--  Holiday Maker - MySQL database
--  Creates the schema, constraints, triggers, a helper view and seed data.
--  Run in MySQL 8.0+ (CHECK constraints are enforced from 8.0.16).
-- =====================================================================

DROP DATABASE IF EXISTS holidaymaker;
CREATE DATABASE holidaymaker
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_swedish_ci;
USE holidaymaker;

-- ---------------------------------------------------------------------
--  Tables
-- ---------------------------------------------------------------------

-- Orter (normaliserat så att samma ort inte stavas olika på olika boenden)
CREATE TABLE cities (
    city_id     INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    CONSTRAINT uq_cities_name UNIQUE (name)
);

-- Boenden (hotell, pensionat ...)
CREATE TABLE accommodations (
    accommodation_id      INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    city_id               INT UNSIGNED NOT NULL,
    name                  VARCHAR(150) NOT NULL,
    has_pool              BOOLEAN NOT NULL DEFAULT FALSE,
    has_restaurant        BOOLEAN NOT NULL DEFAULT FALSE,
    distance_to_beach_m   INT UNSIGNED NOT NULL,
    distance_to_center_m  INT UNSIGNED NOT NULL,
    rating                TINYINT UNSIGNED NOT NULL,
    CONSTRAINT fk_accommodations_city
        FOREIGN KEY (city_id) REFERENCES cities (city_id),
    CONSTRAINT chk_accommodations_rating CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT uq_accommodations_name_city UNIQUE (city_id, name)
);

-- Rum i ett boende
CREATE TABLE rooms (
    room_id           INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    accommodation_id  INT UNSIGNED NOT NULL,
    room_number       VARCHAR(20)  NOT NULL,
    max_guests        TINYINT UNSIGNED NOT NULL,
    price_per_night   DECIMAL(10,2) NOT NULL,
    CONSTRAINT fk_rooms_accommodation
        FOREIGN KEY (accommodation_id) REFERENCES accommodations (accommodation_id)
        ON DELETE CASCADE,
    CONSTRAINT uq_rooms_number UNIQUE (accommodation_id, room_number),
    CONSTRAINT chk_rooms_max_guests CHECK (max_guests > 0),
    CONSTRAINT chk_rooms_price CHECK (price_per_night >= 0)
);

-- Kunder
CREATE TABLE customers (
    customer_id  INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    first_name   VARCHAR(100) NOT NULL,
    last_name    VARCHAR(100) NOT NULL,
    email        VARCHAR(255) NOT NULL,
    phone        VARCHAR(30)  NOT NULL,
    created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_customers_email UNIQUE (email)
);

-- Bokningar: en kund + ett rum + period + antal personer.
-- Totalpriset lagras inte, det räknas ut (nätter * pris per natt)
-- eftersom priset per natt är konstant under perioden.
CREATE TABLE bookings (
    booking_id      INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    customer_id     INT UNSIGNED NOT NULL,
    room_id         INT UNSIGNED NOT NULL,
    check_in_date   DATE NOT NULL,
    check_out_date  DATE NOT NULL,
    guests          TINYINT UNSIGNED NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_bookings_customer
        FOREIGN KEY (customer_id) REFERENCES customers (customer_id),
    CONSTRAINT fk_bookings_room
        FOREIGN KEY (room_id) REFERENCES rooms (room_id),
    CONSTRAINT chk_bookings_dates  CHECK (check_out_date > check_in_date),
    CONSTRAINT chk_bookings_guests CHECK (guests > 0)
);

-- Index som gör tillgänglighetssökningen snabb
CREATE INDEX ix_bookings_room_dates ON bookings (room_id, check_in_date, check_out_date);
CREATE INDEX ix_bookings_customer   ON bookings (customer_id);

-- ---------------------------------------------------------------------
--  Triggers: databasen skyddar sig själv mot överbokning och
--  för många personer, även om applikationen skulle missa en kontroll.
-- ---------------------------------------------------------------------
DELIMITER $$

CREATE TRIGGER trg_bookings_before_insert
BEFORE INSERT ON bookings
FOR EACH ROW
BEGIN
    IF NEW.guests > (SELECT max_guests FROM rooms WHERE room_id = NEW.room_id) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Antalet personer overstiger rummets kapacitet.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM bookings b
        WHERE b.room_id = NEW.room_id
          AND b.check_in_date  < NEW.check_out_date
          AND b.check_out_date > NEW.check_in_date
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Rummet ar redan bokat under perioden.';
    END IF;
END$$

CREATE TRIGGER trg_bookings_before_update
BEFORE UPDATE ON bookings
FOR EACH ROW
BEGIN
    IF NEW.guests > (SELECT max_guests FROM rooms WHERE room_id = NEW.room_id) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Antalet personer overstiger rummets kapacitet.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM bookings b
        WHERE b.room_id = NEW.room_id
          AND b.booking_id <> NEW.booking_id
          AND b.check_in_date  < NEW.check_out_date
          AND b.check_out_date > NEW.check_in_date
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Rummet ar redan bokat under perioden.';
    END IF;
END$$

DELIMITER ;

-- ---------------------------------------------------------------------
--  Vy: bokningar med all info som behövs för att visa dem (punkt 3 och 4)
-- ---------------------------------------------------------------------
CREATE VIEW v_booking_details AS
SELECT
    b.booking_id,
    b.customer_id,
    CONCAT(c.first_name, ' ', c.last_name)            AS customer_name,
    a.name                                            AS accommodation,
    ci.name                                           AS city,
    r.room_number,
    b.check_in_date,
    b.check_out_date,
    DATEDIFF(b.check_out_date, b.check_in_date)       AS nights,
    b.guests,
    r.price_per_night,
    DATEDIFF(b.check_out_date, b.check_in_date) * r.price_per_night AS total_price
FROM bookings b
JOIN customers      c  ON c.customer_id      = b.customer_id
JOIN rooms          r  ON r.room_id          = b.room_id
JOIN accommodations a  ON a.accommodation_id = r.accommodation_id
JOIN cities         ci ON ci.city_id         = a.city_id;