CREATE DATABASE  IF NOT EXISTS `dg_viajes` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `dg_viajes`;
-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: dg_viajes
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `acompanante`
--

DROP TABLE IF EXISTS `acompanante`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `acompanante` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reserva_id` int NOT NULL,
  `dni` varchar(15) COLLATE utf8mb4_general_ci NOT NULL,
  `nombre` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `apellido` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `telefono` varchar(30) COLLATE utf8mb4_general_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_acompanante_dni_reserva` (`reserva_id`,`dni`),
  CONSTRAINT `fk_acompanante_reserva` FOREIGN KEY (`reserva_id`) REFERENCES `reserva` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `acompanante`
--

LOCK TABLES `acompanante` WRITE;
/*!40000 ALTER TABLE `acompanante` DISABLE KEYS */;
INSERT INTO `acompanante` VALUES (1,1,'31222333','Pablo','Fernández','2664-121212'),(2,2,'29444555','Laura','Gómez','2664-232323'),(3,2,'50111222','Mateo','Gómez',NULL),(4,2,'52333444','Emma','Gómez',NULL),(5,3,'36777888','Agustín','López','351-3434343'),(6,3,'37888999','Julieta','López',NULL),(7,3,'48123123','Bautista','López',NULL),(8,3,'49321321','Olivia','López',NULL),(9,5,'41000111','Ignacio','Sosa','2664-565656'),(10,5,'42000222','Rocío','Sosa',NULL),(11,6,'30999888','Pablo','Fernández','2664-121212'),(12,7,'34555666','Sebastián','Álvarez',NULL);
/*!40000 ALTER TABLE `acompanante` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cliente`
--

DROP TABLE IF EXISTS `cliente`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cliente` (
  `id` int NOT NULL AUTO_INCREMENT,
  `dni` varchar(15) COLLATE utf8mb4_general_ci NOT NULL,
  `nombre` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `apellido` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `email` varchar(150) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `telefono` varchar(30) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `domicilio` varchar(200) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_cliente_dni` (`dni`),
  KEY `ix_cliente_apellido` (`apellido`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cliente`
--

LOCK TABLES `cliente` WRITE;
/*!40000 ALTER TABLE `cliente` DISABLE KEYS */;
INSERT INTO `cliente` VALUES (1,'30111222','Lucía','Fernández','lucia.fernandez@example.com','2664-111111','San Martín 120, San Luis',1),(2,'28333444','Martín','Gómez','martin.gomez@example.com','2664-222222','Pringles 455, San Luis',1),(3,'35555666','Valentina','López','valentina.lopez@example.com','351-3333333','Colón 900, Córdoba',1),(4,'32777888','Joaquín','Pérez','joaquin.perez@example.com','261-4444444','Las Heras 77, Mendoza',1),(5,'40999000','Camila','Sosa','camila.sosa@example.com','2664-555555','Rivadavia 33, Villa Mercedes',1),(6,'25123456','Diego','Romero','diego.romero@example.com','11-66666666','Corrientes 1500, CABA',1),(7,'38654321','Florencia','Díaz','florencia.diaz@example.com','2664-777777','Belgrano 210, San Luis',1),(8,'33246810','Nicolás','Álvarez','nicolas.alvarez@example.com','341-8888888','Oroño 400, Rosario',1),(9,'29135791','Paula','Torres','paula.torres@example.com','2664-999999','Junín 15, San Luis',0),(10,'41112233','Tomás','Ruiz',NULL,NULL,NULL,0);
/*!40000 ALTER TABLE `cliente` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `destino`
--

DROP TABLE IF EXISTS `destino`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `destino` (
  `id` int NOT NULL AUTO_INCREMENT,
  `pais` varchar(80) COLLATE utf8mb4_general_ci NOT NULL,
  `provincia` varchar(80) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ciudad` varchar(80) COLLATE utf8mb4_general_ci NOT NULL,
  `descripcion` text COLLATE utf8mb4_general_ci,
  `activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `destino`
--

LOCK TABLES `destino` WRITE;
/*!40000 ALTER TABLE `destino` DISABLE KEYS */;
INSERT INTO `destino` VALUES (1,'Argentina','Río Negro','San Carlos de Bariloche','Lagos, montañas y chocolate en la Patagonia.',1),(2,'Argentina','Mendoza','Mendoza','Bodegas, viñedos y la Cordillera de los Andes.',1),(3,'Argentina','Salta','Salta','Ciudad colonial, peñas y quebradas del norte.',0),(4,'Argentina','Misiones','Puerto Iguazú','Cataratas del Iguazú y selva misionera.',1),(5,'Argentina','Buenos Aires','Mar del Plata','Playas y vida nocturna en la costa atlántica.',1),(6,'Argentina','Tierra del Fuego','Ushuaia','La ciudad más austral del mundo.',0);
/*!40000 ALTER TABLE `destino` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `foto_hospedaje`
--

DROP TABLE IF EXISTS `foto_hospedaje`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `foto_hospedaje` (
  `id` int NOT NULL AUTO_INCREMENT,
  `hospedaje_id` int NOT NULL,
  `archivo` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `es_portada` tinyint(1) NOT NULL DEFAULT '0',
  `subido_en` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_foto_hospedaje` (`hospedaje_id`),
  CONSTRAINT `fk_foto_hospedaje` FOREIGN KEY (`hospedaje_id`) REFERENCES `hospedaje` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `foto_hospedaje`
--

LOCK TABLES `foto_hospedaje` WRITE;
/*!40000 ALTER TABLE `foto_hospedaje` DISABLE KEYS */;
INSERT INTO `foto_hospedaje` VALUES (1,1,'/uploads/hospedajes/1_1.jpg',1,'2026-10-26 19:43:32'),(2,1,'/uploads/hospedajes/1_2.jpg',0,'2026-10-26 19:43:32'),(4,6,'/uploads/hospedajes/6_1.jpg',1,'2026-10-26 19:43:32');
/*!40000 ALTER TABLE `foto_hospedaje` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hospedaje`
--

DROP TABLE IF EXISTS `hospedaje`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hospedaje` (
  `id` int NOT NULL AUTO_INCREMENT,
  `destino_id` int NOT NULL,
  `tipo_id` int NOT NULL,
  `nombre` varchar(120) COLLATE utf8mb4_general_ci NOT NULL,
  `direccion` varchar(200) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `capacidad_maxima` int NOT NULL,
  `servicios` varchar(500) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `precio_noche` decimal(12,2) NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  KEY `fk_hospedaje_destino` (`destino_id`),
  KEY `fk_hospedaje_tipo` (`tipo_id`),
  CONSTRAINT `fk_hospedaje_destino` FOREIGN KEY (`destino_id`) REFERENCES `destino` (`id`),
  CONSTRAINT `fk_hospedaje_tipo` FOREIGN KEY (`tipo_id`) REFERENCES `tipo_hospedaje` (`id`),
  CONSTRAINT `chk_hospedaje_capacidad` CHECK ((`capacidad_maxima` > 0)),
  CONSTRAINT `chk_hospedaje_precio` CHECK ((`precio_noche` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hospedaje`
--

LOCK TABLES `hospedaje` WRITE;
/*!40000 ALTER TABLE `hospedaje` DISABLE KEYS */;
INSERT INTO `hospedaje` VALUES (1,1,1,'Hotel Lago Azul','Av. Bustillo km 3',40,'Desayuno, pileta climatizada, wifi',85000.00,1),(2,1,3,'Cabañas del Bosque','Circuito Chico 1200',6,'Cocina, parrilla, estacionamiento',60000.00,1),(3,2,2,'Hostel Andino','Av. Arístides Villanueva 300',20,'Desayuno, cocina compartida',25000.00,1),(4,2,4,'Departamentos Viñedo','Chacras de Coria 45',4,'Cocina, aire acondicionado, wifi',45000.00,1),(5,3,1,'Hotel Colonial','Caseros 450',30,'Desayuno, wifi, traslado',70000.00,0),(6,4,1,'Hotel Selva','Av. Victoria Aguirre 900',50,'Desayuno, pileta, spa',90000.00,1),(7,4,5,'Casa Cataratas','Los Lapachos 77',8,'Pileta, parrilla, wifi',110000.00,1),(8,5,4,'Departamentos Costa','Bv. Marítimo 2500',5,'Vista al mar, cocina, wifi',50000.00,1),(9,5,2,'Hostel Puerto','12 de Octubre 3100',16,'Desayuno, lockers',22000.00,0),(10,6,1,'Hotel Fin del Mundo','San Martín 800',30,'Desayuno, calefacción, wifi',95000.00,1);
/*!40000 ALTER TABLE `hospedaje` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pago`
--

DROP TABLE IF EXISTS `pago`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pago` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reserva_id` int NOT NULL,
  `tipo` varchar(10) COLLATE utf8mb4_general_ci NOT NULL,
  `fecha` date NOT NULL,
  `importe` decimal(12,2) NOT NULL,
  `medio_pago` varchar(20) COLLATE utf8mb4_general_ci NOT NULL,
  `creado_por` int NOT NULL,
  `creado_en` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `anulado_por` int DEFAULT NULL,
  `anulado_en` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_pago_reserva` (`reserva_id`),
  KEY `fk_pago_creado` (`creado_por`),
  KEY `fk_pago_anulado` (`anulado_por`),
  CONSTRAINT `fk_pago_anulado` FOREIGN KEY (`anulado_por`) REFERENCES `usuario` (`id`),
  CONSTRAINT `fk_pago_creado` FOREIGN KEY (`creado_por`) REFERENCES `usuario` (`id`),
  CONSTRAINT `fk_pago_reserva` FOREIGN KEY (`reserva_id`) REFERENCES `reserva` (`id`),
  CONSTRAINT `chk_pago_importe` CHECK ((`importe` > 0)),
  CONSTRAINT `chk_pago_medio` CHECK ((`medio_pago` in (_utf8mb4'Efectivo',_utf8mb4'Débito',_utf8mb4'Crédito',_utf8mb4'Transferencia'))),
  CONSTRAINT `chk_pago_tipo` CHECK ((`tipo` in (_utf8mb4'Seña',_utf8mb4'Parcial',_utf8mb4'Total')))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pago`
--

LOCK TABLES `pago` WRITE;
/*!40000 ALTER TABLE `pago` DISABLE KEYS */;
INSERT INTO `pago` VALUES (1,1,'Seña','2026-10-05',300000.00,'Transferencia',2,'2026-10-05 10:20:00',NULL,NULL),(2,1,'Parcial','2026-10-15',600000.00,'Efectivo',2,'2026-10-15 17:00:00',NULL,NULL),(3,2,'Total','2026-10-08',2220000.00,'Crédito',2,'2026-10-08 16:45:00',NULL,NULL),(4,4,'Seña','2026-10-01',100000.00,'Efectivo',2,'2026-10-01 09:35:00',1,'2026-10-03 12:00:00'),(5,5,'Seña','2026-10-10',400000.00,'Débito',3,'2026-10-10 18:25:00',NULL,NULL),(6,6,'Seña','2026-10-15',200000.00,'Transferencia',2,'2026-10-15 13:50:00',1,'2026-11-03 16:11:28');
/*!40000 ALTER TABLE `pago` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `paquete`
--

DROP TABLE IF EXISTS `paquete`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `paquete` (
  `id` int NOT NULL AUTO_INCREMENT,
  `destino_id` int NOT NULL,
  `nombre` varchar(120) COLLATE utf8mb4_general_ci NOT NULL,
  `descripcion` text COLLATE utf8mb4_general_ci,
  `precio_persona` decimal(12,2) NOT NULL,
  `fecha_salida` date NOT NULL,
  `fecha_regreso` date NOT NULL,
  `plazas_totales` int NOT NULL,
  `incluye_transporte` tinyint(1) NOT NULL DEFAULT '0',
  `estado` varchar(20) COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'Disponible',
  PRIMARY KEY (`id`),
  KEY `fk_paquete_destino` (`destino_id`),
  KEY `ix_paquete_fecha_salida` (`fecha_salida`),
  CONSTRAINT `fk_paquete_destino` FOREIGN KEY (`destino_id`) REFERENCES `destino` (`id`),
  CONSTRAINT `chk_paquete_estado` CHECK ((`estado` in (_utf8mb4'Disponible',_utf8mb4'Suspendido'))),
  CONSTRAINT `chk_paquete_fechas` CHECK ((`fecha_regreso` > `fecha_salida`)),
  CONSTRAINT `chk_paquete_plazas` CHECK ((`plazas_totales` > 0)),
  CONSTRAINT `chk_paquete_precio` CHECK ((`precio_persona` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `paquete`
--

LOCK TABLES `paquete` WRITE;
/*!40000 ALTER TABLE `paquete` DISABLE KEYS */;
INSERT INTO `paquete` VALUES (1,1,'Bariloche Verano','Siete noches con excursión al Cerro Catedral.',450000.00,'2027-01-10','2027-01-17',30,1,'Disponible'),(2,2,'Mendoza Vendimia','Fiesta de la Vendimia y recorrido por bodegas.',380000.00,'2027-02-26','2027-03-03',20,1,'Disponible'),(3,3,'Salta la Linda','City tour y Quebrada de Humahuaca.',320000.00,'2026-12-05','2026-12-10',25,0,'Disponible'),(4,4,'Cataratas del Iguazú','Circuito argentino y brasileño de cataratas.',520000.00,'2026-11-20','2026-11-24',30,1,'Disponible'),(5,5,'Mar del Plata Enero','Una semana de playa en plena temporada.',280000.00,'2027-01-03','2027-01-10',40,0,'Disponible'),(6,6,'Ushuaia Fin del Mundo','Tren del Fin del Mundo y Canal Beagle.',690000.00,'2027-02-10','2027-02-15',15,1,'Disponible');
/*!40000 ALTER TABLE `paquete` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `reserva`
--

DROP TABLE IF EXISTS `reserva`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `reserva` (
  `id` int NOT NULL AUTO_INCREMENT,
  `cliente_id` int NOT NULL,
  `paquete_id` int NOT NULL,
  `hospedaje_id` int NOT NULL,
  `fecha_reserva` date NOT NULL,
  `cant_pasajeros` int NOT NULL,
  `precio_total` decimal(12,2) NOT NULL,
  `estado` varchar(20) COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'Pendiente',
  `creado_por` int NOT NULL,
  `creado_en` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `modificado_por` int DEFAULT NULL,
  `modificado_en` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_reserva_cliente` (`cliente_id`),
  KEY `fk_reserva_paquete` (`paquete_id`),
  KEY `fk_reserva_hospedaje` (`hospedaje_id`),
  KEY `fk_reserva_creado` (`creado_por`),
  KEY `fk_reserva_modificado` (`modificado_por`),
  CONSTRAINT `fk_reserva_cliente` FOREIGN KEY (`cliente_id`) REFERENCES `cliente` (`id`),
  CONSTRAINT `fk_reserva_creado` FOREIGN KEY (`creado_por`) REFERENCES `usuario` (`id`),
  CONSTRAINT `fk_reserva_hospedaje` FOREIGN KEY (`hospedaje_id`) REFERENCES `hospedaje` (`id`),
  CONSTRAINT `fk_reserva_modificado` FOREIGN KEY (`modificado_por`) REFERENCES `usuario` (`id`),
  CONSTRAINT `fk_reserva_paquete` FOREIGN KEY (`paquete_id`) REFERENCES `paquete` (`id`),
  CONSTRAINT `chk_reserva_estado` CHECK ((`estado` in (_utf8mb4'Pendiente',_utf8mb4'Confirmada',_utf8mb4'Cancelada'))),
  CONSTRAINT `chk_reserva_pasajeros` CHECK ((`cant_pasajeros` > 0)),
  CONSTRAINT `chk_reserva_precio` CHECK ((`precio_total` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `reserva`
--

LOCK TABLES `reserva` WRITE;
/*!40000 ALTER TABLE `reserva` DISABLE KEYS */;
INSERT INTO `reserva` VALUES (1,1,1,1,'2026-10-05',2,1495000.00,'Confirmada',2,'2026-10-05 10:15:00',NULL,NULL),(2,2,1,2,'2026-10-08',4,2220000.00,'Confirmada',2,'2026-10-08 16:40:00',NULL,NULL),(3,3,4,7,'2026-10-12',5,3040000.00,'Pendiente',3,'2026-10-12 11:05:00',NULL,NULL),(4,4,2,3,'2026-10-01',1,505000.00,'Cancelada',2,'2026-10-01 09:30:00',1,'2026-10-03 12:00:00'),(5,5,3,5,'2026-10-10',3,1310000.00,'Confirmada',3,'2026-10-10 18:20:00',NULL,NULL),(6,1,5,8,'2026-10-15',2,910000.00,'Cancelada',2,'2026-10-15 13:45:00',1,'2026-11-03 16:11:28'),(7,8,4,6,'2026-10-27',2,1400000.00,'Cancelada',1,'2026-10-27 15:30:20',1,'2026-10-27 15:30:56');
/*!40000 ALTER TABLE `reserva` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tipo_hospedaje`
--

DROP TABLE IF EXISTS `tipo_hospedaje`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tipo_hospedaje` (
  `id` int NOT NULL AUTO_INCREMENT,
  `nombre` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_tipo_hospedaje_nombre` (`nombre`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tipo_hospedaje`
--

LOCK TABLES `tipo_hospedaje` WRITE;
/*!40000 ALTER TABLE `tipo_hospedaje` DISABLE KEYS */;
INSERT INTO `tipo_hospedaje` VALUES (3,'Cabaña'),(5,'Casa'),(4,'Departamento'),(2,'Hostel'),(1,'Hotel');
/*!40000 ALTER TABLE `tipo_hospedaje` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `usuario`
--

DROP TABLE IF EXISTS `usuario`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuario` (
  `id` int NOT NULL AUTO_INCREMENT,
  `email` varchar(150) COLLATE utf8mb4_general_ci NOT NULL,
  `password_hash` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
  `nombre` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `apellido` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `avatar` varchar(255) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `rol` varchar(20) COLLATE utf8mb4_general_ci NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_usuario_email` (`email`),
  CONSTRAINT `chk_usuario_rol` CHECK ((`rol` in (_utf8mb4'Administrador',_utf8mb4'Empleado')))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `usuario`
--

LOCK TABLES `usuario` WRITE;
/*!40000 ALTER TABLE `usuario` DISABLE KEYS */;
INSERT INTO `usuario` VALUES (1,'admin@dgviajes.com','AQAAAAIAAYagAAAAEHqJ7iUWCL4uCCOQVl1DmoAHIlrwR3rHHx78aADtHq3St20oitiLT5CjbAsOkvS8JQ==','Ana','Admin',NULL,'Administrador',1),(2,'empleado@dgviajes.com','AQAAAAIAAYagAAAAEBVL24mMYkJLlVJYhH1uLmYlsa8eQramVIjfKFNodh5Q0eOmsR1qOdEluxc5o/oYag==','Bruno','Ventas',NULL,'Empleado',1),(3,'sofia@dgviajes.com','AQAAAAIAAYagAAAAEJJc7jKUD/79dfwrMmn1j9lzTUPao/21aZKyuIEgux3MaoJcJAxBr8aB7IgAcOfWtw==','Sofía','Mostrador',NULL,'Empleado',1),(4,'baja@dgviajes.com','AQAAAAIAAYagAAAAEIw/qNnOyPNhwCfZYHZDXsVtTFGejAxKC9c6eZh1cLBJN0Aog8FI17ttwTZXZk21HQ==','Carlos','Inactivo',NULL,'Empleado',0);
/*!40000 ALTER TABLE `usuario` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-03 20:49:26
