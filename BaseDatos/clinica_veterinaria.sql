-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1
-- Tiempo de generación: 30-05-2026 a las 07:58:44
-- Versión del servidor: 10.4.32-MariaDB
-- Versión de PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de datos: `clinica_veterinaria`
--
CREATE DATABASE IF NOT EXISTS `clinica_veterinaria` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `clinica_veterinaria`;

DELIMITER $$
--
-- Procedimientos
--
DROP PROCEDURE IF EXISTS `sp_limpiar_info_clinica`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_limpiar_info_clinica` ()   BEGIN
    IF COALESCE(@CONFIRMAR_BORRADO_TOTAL, '') <> 'SI_BORRAR_TODA_LA_INFO_MENOS_USUARIOS_Y_VETERINARIOS' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Borrado cancelado: establezca la variable de confirmacion correctamente.';
    END IF;

    SET FOREIGN_KEY_CHECKS = 0;

    START TRANSACTION;

    -- Movimientos y operaciones financieras
    DELETE FROM inventario_movimientos;
    DELETE FROM pagos;
    DELETE FROM factura_detalles;
    DELETE FROM cargos_pendientes;
    DELETE FROM facturas;
    DELETE FROM secuencias_documentos;

    -- Seguimiento, órdenes y hospitalización
    DELETE FROM hospitalizacion_evoluciones;
    DELETE FROM hospitalizaciones;
    DELETE FROM catalogo_jaulas;
    DELETE FROM ordenes_clinicas;
    DELETE FROM recordatorios;

    -- Atención clínica
    DELETE FROM receta_detalles;
    DELETE FROM recetas;
    DELETE FROM vacunas_aplicadas;
    DELETE FROM desparasitaciones;
    DELETE FROM consulta_diagnosticos;
    DELETE FROM consulta_servicios;
    DELETE FROM consultas;

    -- Agenda
    DELETE FROM cita_bloques;
    DELETE FROM cita_reagendamientos;
    DELETE FROM cita_historial_estados;
    DELETE FROM citas;

    -- Disponibilidad de profesionales: se conservan los veterinarios,
    -- pero se limpian horarios/bloqueos capturados durante pruebas.
    DELETE FROM veterinario_bloqueos;
    DELETE FROM veterinario_horarios;

    -- Ficha de clientes y pacientes
    DELETE FROM mascota_alertas_clinicas;
    DELETE FROM mascotas;
    DELETE FROM duenos;

    -- Catálogos e inventario: se repoblarán con el script masivo.
    DELETE FROM catalogo_medicamentos;
    DELETE FROM catalogo_vacunas;
    DELETE FROM catalogo_desparasitantes;
    DELETE FROM inventario_lotes;
    DELETE FROM inventario_productos;
    DELETE FROM catalogo_servicios;
    DELETE FROM metodos_pago;
    DELETE FROM tipos_bloqueo;

    COMMIT;

    SET FOREIGN_KEY_CHECKS = 1;

    -- Reinicio de folios internos únicamente en tablas vaciadas.
    ALTER TABLE inventario_movimientos AUTO_INCREMENT = 1;
    ALTER TABLE pagos AUTO_INCREMENT = 1;
    ALTER TABLE factura_detalles AUTO_INCREMENT = 1;
    ALTER TABLE cargos_pendientes AUTO_INCREMENT = 1;
    ALTER TABLE facturas AUTO_INCREMENT = 1;
    ALTER TABLE secuencias_documentos AUTO_INCREMENT = 1;

    ALTER TABLE hospitalizacion_evoluciones AUTO_INCREMENT = 1;
    ALTER TABLE hospitalizaciones AUTO_INCREMENT = 1;
    ALTER TABLE catalogo_jaulas AUTO_INCREMENT = 1;
    ALTER TABLE ordenes_clinicas AUTO_INCREMENT = 1;
    ALTER TABLE recordatorios AUTO_INCREMENT = 1;

    ALTER TABLE receta_detalles AUTO_INCREMENT = 1;
    ALTER TABLE recetas AUTO_INCREMENT = 1;
    ALTER TABLE vacunas_aplicadas AUTO_INCREMENT = 1;
    ALTER TABLE desparasitaciones AUTO_INCREMENT = 1;
    ALTER TABLE consulta_diagnosticos AUTO_INCREMENT = 1;
    ALTER TABLE consulta_servicios AUTO_INCREMENT = 1;
    ALTER TABLE consultas AUTO_INCREMENT = 1;

    ALTER TABLE cita_bloques AUTO_INCREMENT = 1;
    ALTER TABLE cita_reagendamientos AUTO_INCREMENT = 1;
    ALTER TABLE cita_historial_estados AUTO_INCREMENT = 1;
    ALTER TABLE citas AUTO_INCREMENT = 1;

    ALTER TABLE veterinario_bloqueos AUTO_INCREMENT = 1;
    ALTER TABLE veterinario_horarios AUTO_INCREMENT = 1;

    ALTER TABLE mascota_alertas_clinicas AUTO_INCREMENT = 1;
    ALTER TABLE mascotas AUTO_INCREMENT = 1;
    ALTER TABLE duenos AUTO_INCREMENT = 1;

    ALTER TABLE catalogo_medicamentos AUTO_INCREMENT = 1;
    ALTER TABLE catalogo_vacunas AUTO_INCREMENT = 1;
    ALTER TABLE catalogo_desparasitantes AUTO_INCREMENT = 1;
    ALTER TABLE inventario_lotes AUTO_INCREMENT = 1;
    ALTER TABLE inventario_productos AUTO_INCREMENT = 1;
    ALTER TABLE catalogo_servicios AUTO_INCREMENT = 1;
    ALTER TABLE metodos_pago AUTO_INCREMENT = 1;
    ALTER TABLE tipos_bloqueo AUTO_INCREMENT = 1;

    SELECT 'LIMPIEZA TERMINADA' AS resultado,
           'Se conservaron roles, usuarios y veterinarios. Se eliminaron todos los demás datos.' AS detalle;
END$$

DELIMITER ;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cargos_pendientes`
--

DROP TABLE IF EXISTS `cargos_pendientes`;
CREATE TABLE `cargos_pendientes` (
  `id_cargo` bigint(20) UNSIGNED NOT NULL,
  `id_dueno` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED DEFAULT NULL,
  `id_consulta` bigint(20) UNSIGNED DEFAULT NULL,
  `id_cita` bigint(20) UNSIGNED DEFAULT NULL,
  `tipo_item` enum('Servicio','Medicamento','Vacuna','Desparasitante','Producto','Laboratorio','Hospitalización','Otro') NOT NULL,
  `id_referencia` bigint(20) UNSIGNED DEFAULT NULL,
  `descripcion` varchar(250) NOT NULL,
  `cantidad` decimal(10,2) NOT NULL DEFAULT 1.00,
  `precio_unitario` decimal(12,2) NOT NULL DEFAULT 0.00,
  `descuento` decimal(12,2) NOT NULL DEFAULT 0.00,
  `subtotal` decimal(12,2) NOT NULL DEFAULT 0.00,
  `estado` enum('Pendiente','Facturado','Anulado') NOT NULL DEFAULT 'Pendiente',
  `id_factura` bigint(20) UNSIGNED DEFAULT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `cargos_pendientes`
--

INSERT INTO `cargos_pendientes` (`id_cargo`, `id_dueno`, `id_mascota`, `id_consulta`, `id_cita`, `tipo_item`, `id_referencia`, `descripcion`, `cantidad`, `precio_unitario`, `descuento`, `subtotal`, `estado`, `id_factura`, `fecha_creacion`) VALUES
(370, 224, 307, NULL, NULL, 'Servicio', NULL, 'Control preventivo pendiente de facturar', 1.00, 180.00, 0.00, 180.00, 'Facturado', 221, '2026-05-27 22:50:37'),
(371, 225, 308, NULL, NULL, 'Servicio', NULL, 'Control preventivo pendiente de facturar', 1.00, 180.00, 0.00, 180.00, 'Pendiente', NULL, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `catalogo_desparasitantes`
--

DROP TABLE IF EXISTS `catalogo_desparasitantes`;
CREATE TABLE `catalogo_desparasitantes` (
  `id_desparasitante` int(10) UNSIGNED NOT NULL,
  `codigo` varchar(25) NOT NULL,
  `nombre` varchar(150) NOT NULL,
  `presentacion` varchar(120) DEFAULT NULL,
  `dosis_sugerida` varchar(100) DEFAULT NULL,
  `intervalo_dias_sugerido` int(11) DEFAULT NULL,
  `precio_base` decimal(12,2) NOT NULL DEFAULT 0.00,
  `controla_inventario` tinyint(1) NOT NULL DEFAULT 1,
  `id_producto_inventario` bigint(20) UNSIGNED DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `catalogo_desparasitantes`
--

INSERT INTO `catalogo_desparasitantes` (`id_desparasitante`, `codigo`, `nombre`, `presentacion`, `dosis_sugerida`, `intervalo_dias_sugerido`, `precio_base`, `controla_inventario`, `id_producto_inventario`, `activo`) VALUES
(20, 'DES-000001', 'Desparasitante oral amplio espectro', 'Tableta', '1 tableta por cada 10 kg', 90, 75.00, 1, 68, 1),
(21, 'DES-000002', 'Suspensión para cachorro', 'Frasco 30 ml', '1 ml por cada 2 kg', 30, 110.00, 1, 69, 1),
(22, 'DES-000003', 'Pipeta antiparasitaria externa', 'Pipeta', 'Según peso del paciente', 30, 120.00, 1, 70, 1),
(23, 'DES-000004', 'Antiparasitario felino oral', 'Tableta', 'Media tableta hasta 5 kg', 90, 78.00, 0, NULL, 1),
(24, 'DES-000005', 'Desparasitante interno cachorro', 'Pasta oral', 'Según peso', 30, 95.00, 0, NULL, 1),
(25, 'DES-000006', 'Control pulgas y garrapatas', 'Tableta masticable', 'Según presentación por peso', 30, 180.00, 0, NULL, 1),
(26, 'DES-000007', 'Tratamiento giardiasis', 'Suspensión', 'Según valoración veterinaria', 15, 145.00, 0, NULL, 1),
(27, 'DES-000008', 'Antiparasitario externo felino', 'Pipeta', 'Una pipeta según peso', 30, 135.00, 0, NULL, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `catalogo_jaulas`
--

DROP TABLE IF EXISTS `catalogo_jaulas`;
CREATE TABLE `catalogo_jaulas` (
  `id_jaula` int(10) UNSIGNED NOT NULL,
  `codigo_jaula` varchar(20) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `tipo` enum('Consulta','Observación','Hospitalización','Aislamiento','Recuperación') NOT NULL DEFAULT 'Hospitalización',
  `ubicacion` varchar(100) DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `catalogo_jaulas`
--

INSERT INTO `catalogo_jaulas` (`id_jaula`, `codigo_jaula`, `nombre`, `tipo`, `ubicacion`, `activo`, `fecha_creacion`) VALUES
(27, 'JAU-000001', 'Observación Canina 01', 'Observación', 'Área clínica', 1, '2026-05-27 22:50:37'),
(28, 'JAU-000002', 'Observación Canina 02', 'Observación', 'Área clínica', 1, '2026-05-27 22:50:37'),
(29, 'JAU-000003', 'Hospitalización Canina 01', 'Hospitalización', 'Hospitalización canina', 1, '2026-05-27 22:50:37'),
(30, 'JAU-000004', 'Hospitalización Canina 02', 'Hospitalización', 'Hospitalización canina', 1, '2026-05-27 22:50:37'),
(31, 'JAU-000005', 'Hospitalización Felina 01', 'Hospitalización', 'Hospitalización felina', 1, '2026-05-27 22:50:37'),
(32, 'JAU-000006', 'Hospitalización Felina 02', 'Hospitalización', 'Hospitalización felina', 1, '2026-05-27 22:50:37'),
(33, 'JAU-000007', 'Aislamiento 01', 'Aislamiento', 'Área aislada', 1, '2026-05-27 22:50:37'),
(34, 'JAU-000008', 'Recuperación 01', 'Recuperación', 'Posprocedimiento', 1, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `catalogo_medicamentos`
--

DROP TABLE IF EXISTS `catalogo_medicamentos`;
CREATE TABLE `catalogo_medicamentos` (
  `id_medicamento` int(10) UNSIGNED NOT NULL,
  `codigo` varchar(25) NOT NULL,
  `nombre` varchar(150) NOT NULL,
  `presentacion` varchar(120) DEFAULT NULL,
  `concentracion` varchar(80) DEFAULT NULL,
  `via_administracion` varchar(100) DEFAULT NULL,
  `indicaciones_predeterminadas` text DEFAULT NULL,
  `precio_venta` decimal(12,2) NOT NULL DEFAULT 0.00,
  `controla_inventario` tinyint(1) NOT NULL DEFAULT 0,
  `id_producto_inventario` bigint(20) UNSIGNED DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `catalogo_medicamentos`
--

INSERT INTO `catalogo_medicamentos` (`id_medicamento`, `codigo`, `nombre`, `presentacion`, `concentracion`, `via_administracion`, `indicaciones_predeterminadas`, `precio_venta`, `controla_inventario`, `id_producto_inventario`, `activo`) VALUES
(30, 'MED-000001', 'Amoxicilina', 'Tabletas', '250 mg', 'Oral', 'Administrar después del alimento.', 125.00, 1, 59, 1),
(31, 'MED-000002', 'Meloxicam', 'Suspensión', '1.5 mg/ml', 'Oral', 'Administrar según peso y prescripción.', 155.00, 1, 60, 1),
(32, 'MED-000003', 'Cefalexina', 'Tabletas', '500 mg', 'Oral', 'Completar el tratamiento indicado.', 165.00, 1, 61, 1),
(33, 'MED-000004', 'Clorhexidina', 'Solución', '2%', 'Tópica', 'Aplicar en zona indicada.', 90.00, 1, 62, 1),
(34, 'MED-000005', 'Omeprazol veterinario', 'Cápsula', '20 mg', 'Oral', 'Administrar antes del alimento.', 85.00, 0, NULL, 1),
(35, 'MED-000006', 'Prednisolona', 'Tableta', '5 mg', 'Oral', 'Administrar según pauta médica.', 98.00, 0, NULL, 1),
(36, 'MED-000007', 'Metronidazol', 'Tableta', '250 mg', 'Oral', 'Administrar con alimento.', 110.00, 0, NULL, 1),
(37, 'MED-000008', 'Sucralfato', 'Suspensión', '1 g/10 ml', 'Oral', 'Administrar separado de otros medicamentos.', 140.00, 0, NULL, 1),
(38, 'MED-000009', 'Tramadol', 'Tableta', '50 mg', 'Oral', 'Utilizar únicamente bajo indicación profesional.', 130.00, 0, NULL, 1),
(39, 'MED-000010', 'Medicamento libre en receta', NULL, NULL, NULL, NULL, 0.00, 0, NULL, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `catalogo_servicios`
--

DROP TABLE IF EXISTS `catalogo_servicios`;
CREATE TABLE `catalogo_servicios` (
  `id_servicio` int(10) UNSIGNED NOT NULL,
  `codigo` varchar(20) NOT NULL,
  `nombre` varchar(120) NOT NULL,
  `descripcion` varchar(350) DEFAULT NULL,
  `precio_base` decimal(12,2) NOT NULL DEFAULT 0.00,
  `duracion_minutos` int(11) NOT NULL,
  `genera_cargo` tinyint(1) NOT NULL DEFAULT 1,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `catalogo_servicios`
--

INSERT INTO `catalogo_servicios` (`id_servicio`, `codigo`, `nombre`, `descripcion`, `precio_base`, `duracion_minutos`, `genera_cargo`, `activo`) VALUES
(59, 'SER-000001', 'Consulta general', 'Evaluación clínica general.', 180.00, 30, 1, 1),
(60, 'SER-000002', 'Consulta especializada', 'Evaluación con enfoque especializado.', 300.00, 60, 1, 1),
(61, 'SER-000003', 'Consulta de urgencia', 'Atención prioritaria de urgencia.', 450.00, 60, 1, 1),
(62, 'SER-000004', 'Control de cachorro', 'Seguimiento de crecimiento y plan preventivo.', 160.00, 30, 1, 1),
(63, 'SER-000005', 'Control geriátrico', 'Evaluación preventiva del paciente senior.', 320.00, 60, 1, 1),
(64, 'SER-000006', 'Consulta dermatológica', 'Valoración de piel y pelo.', 340.00, 60, 1, 1),
(65, 'SER-000007', 'Consulta cardiológica', 'Evaluación cardiovascular inicial.', 420.00, 60, 1, 1),
(66, 'SER-000008', 'Vacunación', 'Aplicación y registro de vacuna.', 95.00, 30, 1, 1),
(67, 'SER-000009', 'Desparasitación', 'Aplicación de producto antiparasitario.', 75.00, 30, 1, 1),
(68, 'SER-000010', 'Curación simple', 'Limpieza y curación menor.', 135.00, 30, 1, 1),
(69, 'SER-000011', 'Curación avanzada', 'Procedimiento con vendaje y control.', 230.00, 60, 1, 1),
(70, 'SER-000012', 'Toma de muestra', 'Toma de sangre u otra muestra.', 95.00, 30, 1, 1),
(71, 'SER-000013', 'Radiografía', 'Estudio radiográfico simple.', 360.00, 60, 1, 1),
(72, 'SER-000014', 'Ultrasonido abdominal', 'Estudio de imagen abdominal.', 480.00, 60, 1, 1),
(73, 'SER-000015', 'Limpieza dental', 'Profilaxis dental.', 650.00, 90, 1, 1),
(74, 'SER-000016', 'Cirugía menor', 'Procedimiento quirúrgico menor.', 950.00, 120, 1, 1),
(75, 'SER-000017', 'Esterilización hembra', 'Procedimiento quirúrgico programado.', 1600.00, 120, 1, 1),
(76, 'SER-000018', 'Castración macho', 'Procedimiento quirúrgico programado.', 1200.00, 90, 1, 1),
(77, 'SER-000019', 'Hospitalización diaria', 'Monitoreo hospitalario por día.', 420.00, 1440, 1, 1),
(78, 'SER-000020', 'Certificado de salud', 'Examen y emisión de certificado.', 220.00, 30, 1, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `catalogo_vacunas`
--

DROP TABLE IF EXISTS `catalogo_vacunas`;
CREATE TABLE `catalogo_vacunas` (
  `id_vacuna` int(10) UNSIGNED NOT NULL,
  `codigo` varchar(25) NOT NULL,
  `nombre` varchar(150) NOT NULL,
  `especie_aplicable` varchar(80) DEFAULT NULL,
  `descripcion` varchar(300) DEFAULT NULL,
  `intervalo_dias_sugerido` int(11) DEFAULT NULL,
  `precio_base` decimal(12,2) NOT NULL DEFAULT 0.00,
  `controla_inventario` tinyint(1) NOT NULL DEFAULT 1,
  `id_producto_inventario` bigint(20) UNSIGNED DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `catalogo_vacunas`
--

INSERT INTO `catalogo_vacunas` (`id_vacuna`, `codigo`, `nombre`, `especie_aplicable`, `descripcion`, `intervalo_dias_sugerido`, `precio_base`, `controla_inventario`, `id_producto_inventario`, `activo`) VALUES
(26, 'VAC-000001', 'Antirrábica', 'Canino/Felino', 'Prevención anual contra rabia.', 365, 180.00, 1, 63, 1),
(27, 'VAC-000002', 'Múltiple canina DHPP', 'Canino', 'Protección contra distemper, hepatitis, parvovirus y parainfluenza.', 365, 230.00, 1, 64, 1),
(28, 'VAC-000003', 'Triple felina', 'Felino', 'Protección básica felina.', 365, 245.00, 1, 65, 1),
(29, 'VAC-000004', 'Bordetella', 'Canino', 'Prevención de tos de las perreras.', 365, 215.00, 1, 66, 1),
(30, 'VAC-000005', 'Leucemia felina', 'Felino', 'Prevención FeLV.', 365, 270.00, 1, 67, 1),
(31, 'VAC-000006', 'Coronavirus canino', 'Canino', 'Protección complementaria según riesgo.', 365, 210.00, 0, NULL, 1),
(32, 'VAC-000007', 'Giardia', 'Canino', 'Vacunación complementaria según evaluación.', 365, 225.00, 0, NULL, 1),
(33, 'VAC-000008', 'Séxtuple canina', 'Canino', 'Refuerzo ampliado para caninos.', 365, 285.00, 0, NULL, 1),
(34, 'VAC-000009', 'Cuádruple felina', 'Felino', 'Plan preventivo ampliado.', 365, 290.00, 0, NULL, 1),
(35, 'VAC-000010', 'Vacuna para cachorro - primera dosis', 'Canino', 'Inicio de esquema de cachorro.', 21, 195.00, 0, NULL, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `citas`
--

DROP TABLE IF EXISTS `citas`;
CREATE TABLE `citas` (
  `id_cita` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `id_servicio` int(10) UNSIGNED NOT NULL,
  `fecha_hora_inicio` datetime NOT NULL,
  `duracion_minutos` int(11) NOT NULL,
  `fecha_hora_fin` datetime NOT NULL,
  `motivo_consulta` varchar(500) NOT NULL,
  `observaciones_recepcion` text DEFAULT NULL,
  `estado` enum('Pendiente','Confirmada','Llegó','En consulta','Atendida','Cancelada','No asistió','Reagendada') NOT NULL DEFAULT 'Pendiente',
  `motivo_cancelacion` varchar(500) DEFAULT NULL,
  `motivo_no_asistencia` varchar(500) DEFAULT NULL,
  `fecha_llegada` datetime DEFAULT NULL,
  `fecha_inicio_consulta` datetime DEFAULT NULL,
  `fecha_finalizacion` datetime DEFAULT NULL,
  `id_usuario_creacion` int(10) UNSIGNED NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `id_usuario_modificacion` int(10) UNSIGNED DEFAULT NULL,
  `fecha_modificacion` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `citas`
--

INSERT INTO `citas` (`id_cita`, `id_mascota`, `id_veterinario`, `id_servicio`, `fecha_hora_inicio`, `duracion_minutos`, `fecha_hora_fin`, `motivo_consulta`, `observaciones_recepcion`, `estado`, `motivo_cancelacion`, `motivo_no_asistencia`, `fecha_llegada`, `fecha_inicio_consulta`, `fecha_finalizacion`, `id_usuario_creacion`, `fecha_creacion`, `id_usuario_modificacion`, `fecha_modificacion`) VALUES
(246, 288, 1, 66, '2026-05-04 09:00:00', 30, '2026-05-04 09:30:00', 'Refuerzo anual de vacunación.', 'HIST-001', 'Atendida', NULL, NULL, '2026-05-04 08:55:00', '2026-05-04 09:00:00', '2026-05-04 09:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(247, 289, 1, 64, '2026-05-05 10:00:00', 60, '2026-05-05 11:00:00', 'Prurito y lesiones cutáneas.', 'HIST-002', 'Atendida', NULL, NULL, '2026-05-05 09:55:00', '2026-05-05 10:00:00', '2026-05-05 11:00:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(248, 290, 2, 59, '2026-05-06 11:30:00', 30, '2026-05-06 12:00:00', 'Revisión preventiva felina.', 'HIST-003', 'Atendida', NULL, NULL, '2026-05-06 11:25:00', '2026-05-06 11:30:00', '2026-05-06 12:00:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(249, 291, 2, 67, '2026-05-07 09:00:00', 30, '2026-05-07 09:30:00', 'Desparasitación preventiva.', 'HIST-004', 'Atendida', NULL, NULL, '2026-05-07 08:55:00', '2026-05-07 09:00:00', '2026-05-07 09:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(250, 292, 1, 60, '2026-05-11 09:30:00', 60, '2026-05-11 10:30:00', 'Control de peso y articulaciones.', 'HIST-005', 'Atendida', NULL, NULL, '2026-05-11 09:25:00', '2026-05-11 09:30:00', '2026-05-11 10:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(251, 293, 2, 66, '2026-05-12 10:00:00', 30, '2026-05-12 10:30:00', 'Vacuna triple felina.', 'HIST-006', 'Atendida', NULL, NULL, '2026-05-12 09:55:00', '2026-05-12 10:00:00', '2026-05-12 10:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(252, 294, 1, 68, '2026-05-13 15:00:00', 30, '2026-05-13 15:30:00', 'Herida superficial en pata.', 'HIST-007', 'Atendida', NULL, NULL, '2026-05-13 14:55:00', '2026-05-13 15:00:00', '2026-05-13 15:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(253, 295, 2, 70, '2026-05-14 12:00:00', 30, '2026-05-14 12:30:00', 'Toma de muestra para hemograma.', 'HIST-008', 'Atendida', NULL, NULL, '2026-05-14 11:55:00', '2026-05-14 12:00:00', '2026-05-14 12:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(254, 296, 1, 59, '2026-05-18 08:30:00', 30, '2026-05-18 09:00:00', 'Control de cachorro.', 'HIST-009', 'Atendida', NULL, NULL, '2026-05-18 08:25:00', '2026-05-18 08:30:00', '2026-05-18 09:00:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(255, 297, 2, 60, '2026-05-19 13:00:00', 60, '2026-05-19 14:00:00', 'Evaluación renal preventiva.', 'HIST-010', 'Atendida', NULL, NULL, '2026-05-19 12:55:00', '2026-05-19 13:00:00', '2026-05-19 14:00:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(256, 298, 1, 66, '2026-05-20 09:00:00', 30, '2026-05-20 09:30:00', 'Vacunación felina anual.', 'HIST-011', 'Atendida', NULL, NULL, '2026-05-20 08:55:00', '2026-05-20 09:00:00', '2026-05-20 09:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(257, 299, 2, 71, '2026-05-21 10:00:00', 60, '2026-05-21 11:00:00', 'Cojera miembro posterior.', 'HIST-012', 'Atendida', NULL, NULL, '2026-05-21 09:55:00', '2026-05-21 10:00:00', '2026-05-21 11:00:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(258, 300, 1, 59, '2026-05-25 09:00:00', 30, '2026-05-25 09:30:00', 'Revisión general.', 'HIST-013', 'Atendida', NULL, NULL, '2026-05-25 08:55:00', '2026-05-25 09:00:00', '2026-05-25 09:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(259, 301, 2, 60, '2026-05-26 09:30:00', 60, '2026-05-26 10:30:00', 'Control ortopédico.', 'HIST-014', 'Atendida', NULL, NULL, '2026-05-26 09:25:00', '2026-05-26 09:30:00', '2026-05-26 10:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(260, 302, 1, 66, '2026-05-27 11:00:00', 30, '2026-05-27 11:30:00', 'Refuerzo múltiple canino.', 'HIST-015', 'Atendida', NULL, NULL, '2026-05-27 10:55:00', '2026-05-27 11:00:00', '2026-05-27 11:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(261, 303, 2, 67, '2026-05-28 15:30:00', 30, '2026-05-28 16:00:00', 'Desparasitación trimestral.', 'HIST-016', 'Atendida', NULL, NULL, '2026-05-28 15:25:00', '2026-05-28 15:30:00', '2026-05-28 16:00:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(262, 304, 1, 59, '2026-05-29 09:00:00', 30, '2026-05-29 09:30:00', 'Revisión general.', 'HIST-017', 'Cancelada', 'Propietario solicitó cambio de fecha.', NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(263, 305, 2, 59, '2026-05-22 10:00:00', 30, '2026-05-22 10:30:00', 'Consulta general.', 'HIST-018', 'No asistió', NULL, 'Paciente no se presentó.', NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(264, 306, 1, 59, '2026-05-15 12:00:00', 30, '2026-05-15 12:30:00', 'Control felino.', 'HIST-019', 'Cancelada', 'Cambio de disponibilidad del propietario.', NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(265, 307, 2, 59, '2026-05-08 16:00:00', 30, '2026-05-08 16:30:00', 'Valoración preventiva de ave.', 'HIST-020', 'Atendida', NULL, NULL, '2026-05-08 15:55:00', '2026-05-08 16:00:00', '2026-05-08 16:30:00', 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(266, 288, 1, 59, '2026-06-01 09:00:00', 30, '2026-06-01 09:30:00', 'Control general anual.', 'FUT-001', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(267, 289, 1, 64, '2026-06-01 10:00:00', 60, '2026-06-01 11:00:00', 'Seguimiento dermatológico.', 'FUT-002', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(268, 290, 2, 66, '2026-06-01 09:00:00', 30, '2026-06-01 09:30:00', 'Vacuna anual felina.', 'FUT-003', 'Pendiente', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(269, 291, 2, 67, '2026-06-02 08:30:00', 30, '2026-06-02 09:00:00', 'Desparasitación preventiva.', 'FUT-004', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(270, 292, 1, 60, '2026-06-02 11:00:00', 60, '2026-06-02 12:00:00', 'Control de condición crónica.', 'FUT-005', 'Pendiente', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(271, 293, 2, 59, '2026-06-03 10:30:00', 30, '2026-06-03 11:00:00', 'Revisión general felina.', 'FUT-006', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(272, 294, 1, 68, '2026-06-03 14:00:00', 30, '2026-06-03 14:30:00', 'Revisión de herida.', 'FUT-007', 'Pendiente', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(273, 295, 2, 59, '2026-06-04 09:30:00', 30, '2026-06-04 10:00:00', 'Consulta preventiva.', 'FUT-008', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(274, 296, 1, 66, '2026-06-04 15:00:00', 30, '2026-06-04 15:30:00', 'Esquema cachorro.', 'FUT-009', 'Pendiente', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(275, 297, 2, 60, '2026-06-05 12:00:00', 60, '2026-06-05 13:00:00', 'Evaluación clínica felina.', 'FUT-010', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(276, 298, 1, 66, '2026-06-08 09:00:00', 30, '2026-06-08 09:30:00', 'Vacuna felina.', 'FUT-011', 'Pendiente', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(277, 309, 2, 59, '2026-06-09 16:00:00', 30, '2026-06-09 16:30:00', 'Control preventivo de conejo.', 'FUT-012', 'Confirmada', NULL, NULL, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cita_bloques`
--

DROP TABLE IF EXISTS `cita_bloques`;
CREATE TABLE `cita_bloques` (
  `id_bloque` bigint(20) UNSIGNED NOT NULL,
  `id_cita` bigint(20) UNSIGNED NOT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `fecha_hora_bloque` datetime NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `cita_bloques`
--

INSERT INTO `cita_bloques` (`id_bloque`, `id_cita`, `id_veterinario`, `fecha_hora_bloque`, `fecha_creacion`) VALUES
(71, 266, 1, '2026-06-01 09:00:00', '2026-05-27 22:50:37'),
(72, 267, 1, '2026-06-01 10:00:00', '2026-05-27 22:50:37'),
(73, 268, 2, '2026-06-01 09:00:00', '2026-05-27 22:50:37'),
(74, 269, 2, '2026-06-02 08:30:00', '2026-05-27 22:50:37'),
(75, 270, 1, '2026-06-02 11:00:00', '2026-05-27 22:50:37'),
(76, 271, 2, '2026-06-03 10:30:00', '2026-05-27 22:50:37'),
(77, 272, 1, '2026-06-03 14:00:00', '2026-05-27 22:50:37'),
(78, 273, 2, '2026-06-04 09:30:00', '2026-05-27 22:50:37'),
(79, 274, 1, '2026-06-04 15:00:00', '2026-05-27 22:50:37'),
(80, 275, 2, '2026-06-05 12:00:00', '2026-05-27 22:50:37'),
(81, 276, 1, '2026-06-08 09:00:00', '2026-05-27 22:50:37'),
(82, 277, 2, '2026-06-09 16:00:00', '2026-05-27 22:50:37'),
(83, 267, 1, '2026-06-01 10:30:00', '2026-05-27 22:50:37'),
(84, 270, 1, '2026-06-02 11:30:00', '2026-05-27 22:50:37'),
(85, 275, 2, '2026-06-05 12:30:00', '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cita_historial_estados`
--

DROP TABLE IF EXISTS `cita_historial_estados`;
CREATE TABLE `cita_historial_estados` (
  `id_historial` bigint(20) UNSIGNED NOT NULL,
  `id_cita` bigint(20) UNSIGNED NOT NULL,
  `estado_anterior` varchar(30) DEFAULT NULL,
  `estado_nuevo` varchar(30) NOT NULL,
  `motivo` varchar(500) DEFAULT NULL,
  `id_usuario` int(10) UNSIGNED NOT NULL,
  `fecha_registro` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `cita_historial_estados`
--

INSERT INTO `cita_historial_estados` (`id_historial`, `id_cita`, `estado_anterior`, `estado_nuevo`, `motivo`, `id_usuario`, `fecha_registro`) VALUES
(351, 246, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(352, 247, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(353, 248, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(354, 249, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(355, 250, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(356, 251, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(357, 252, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(358, 253, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(359, 254, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(360, 255, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(361, 256, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(362, 257, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(363, 258, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(364, 259, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(365, 260, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(366, 261, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(367, 262, NULL, 'Cancelada', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(368, 263, NULL, 'No asistió', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(369, 264, NULL, 'Cancelada', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(370, 265, NULL, 'Atendida', 'Registro histórico inicial de demostración.', 1, '2026-05-27 22:50:37'),
(382, 266, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(383, 267, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(384, 268, NULL, 'Pendiente', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(385, 269, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(386, 270, NULL, 'Pendiente', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(387, 271, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(388, 272, NULL, 'Pendiente', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(389, 273, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(390, 274, NULL, 'Pendiente', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(391, 275, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(392, 276, NULL, 'Pendiente', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37'),
(393, 277, NULL, 'Confirmada', 'Cita futura precargada para demostración.', 1, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cita_reagendamientos`
--

DROP TABLE IF EXISTS `cita_reagendamientos`;
CREATE TABLE `cita_reagendamientos` (
  `id_reagendamiento` bigint(20) UNSIGNED NOT NULL,
  `id_cita` bigint(20) UNSIGNED NOT NULL,
  `fecha_hora_anterior` datetime NOT NULL,
  `fecha_hora_nueva` datetime NOT NULL,
  `duracion_anterior` int(11) NOT NULL,
  `duracion_nueva` int(11) NOT NULL,
  `id_veterinario_anterior` int(10) UNSIGNED NOT NULL,
  `id_veterinario_nuevo` int(10) UNSIGNED NOT NULL,
  `motivo` varchar(500) NOT NULL,
  `id_usuario` int(10) UNSIGNED NOT NULL,
  `fecha_registro` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `consultas`
--

DROP TABLE IF EXISTS `consultas`;
CREATE TABLE `consultas` (
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `id_cita` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `fecha_atencion` datetime NOT NULL DEFAULT current_timestamp(),
  `motivo_consulta` varchar(500) NOT NULL,
  `anamnesis` text DEFAULT NULL,
  `peso` decimal(8,2) DEFAULT NULL,
  `temperatura` decimal(5,2) DEFAULT NULL,
  `frecuencia_cardiaca` smallint(5) UNSIGNED DEFAULT NULL,
  `frecuencia_respiratoria` smallint(5) UNSIGNED DEFAULT NULL,
  `hidratacion` varchar(100) DEFAULT NULL,
  `hallazgos_fisicos` text DEFAULT NULL,
  `pronostico` text DEFAULT NULL,
  `tratamiento_general` text DEFAULT NULL,
  `indicaciones` text DEFAULT NULL,
  `proxima_revision` datetime DEFAULT NULL,
  `estado_egreso` varchar(100) DEFAULT NULL,
  `id_usuario_creacion` int(10) UNSIGNED NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `fecha_modificacion` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `consultas`
--

INSERT INTO `consultas` (`id_consulta`, `id_cita`, `id_mascota`, `id_veterinario`, `fecha_atencion`, `motivo_consulta`, `anamnesis`, `peso`, `temperatura`, `frecuencia_cardiaca`, `frecuencia_respiratoria`, `hidratacion`, `hallazgos_fisicos`, `pronostico`, `tratamiento_general`, `indicaciones`, `proxima_revision`, `estado_egreso`, `id_usuario_creacion`, `fecha_creacion`, `fecha_modificacion`) VALUES
(190, 246, 288, 1, '2026-05-04 09:00:00', 'Refuerzo anual de vacunación.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 24.50, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-03 09:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(191, 247, 289, 1, '2026-05-05 10:00:00', 'Prurito y lesiones cutáneas.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 32.80, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-04 10:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(192, 248, 290, 2, '2026-05-06 11:30:00', 'Revisión preventiva felina.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 4.30, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-05 11:30:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(193, 249, 291, 2, '2026-05-07 09:00:00', 'Desparasitación preventiva.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 8.70, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-06 09:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(194, 250, 292, 1, '2026-05-11 09:30:00', 'Control de peso y articulaciones.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 22.10, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-10 09:30:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(195, 251, 293, 2, '2026-05-12 10:00:00', 'Vacuna triple felina.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 4.80, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-11 10:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(196, 252, 294, 1, '2026-05-13 15:00:00', 'Herida superficial en pata.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 13.40, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-12 15:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(197, 253, 295, 2, '2026-05-14 12:00:00', 'Toma de muestra para hemograma.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 21.90, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-13 12:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(198, 254, 296, 1, '2026-05-18 08:30:00', 'Control de cachorro.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 3.20, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-17 08:30:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(199, 255, 297, 2, '2026-05-19 13:00:00', 'Evaluación renal preventiva.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 5.10, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-18 13:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(200, 256, 298, 1, '2026-05-20 09:00:00', 'Vacunación felina anual.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 5.80, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-19 09:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(201, 257, 299, 2, '2026-05-21 10:00:00', 'Cojera miembro posterior.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 26.30, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-20 10:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(202, 258, 300, 1, '2026-05-25 09:00:00', 'Revisión general.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 4.60, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-24 09:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(203, 259, 301, 2, '2026-05-26 09:30:00', 'Control ortopédico.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 39.20, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-25 09:30:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(204, 260, 302, 1, '2026-05-27 11:00:00', 'Refuerzo múltiple canino.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 15.60, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-26 11:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(205, 261, 303, 2, '2026-05-28 15:30:00', 'Desparasitación trimestral.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 28.00, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-27 15:30:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(206, 265, 307, 2, '2026-05-08 16:00:00', 'Valoración preventiva de ave.', 'Paciente acude a atención programada; propietario aporta antecedentes y evolución.', 0.12, 38.50, 92, 24, 'Normal', 'Evaluación física realizada; hallazgos compatibles con el motivo de consulta.', 'Favorable', 'Tratamiento ambulatorio según evaluación.', 'Mantener vigilancia y acudir a revisión si presenta cambios.', '2026-06-07 16:00:00', 'Estable', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `consulta_diagnosticos`
--

DROP TABLE IF EXISTS `consulta_diagnosticos`;
CREATE TABLE `consulta_diagnosticos` (
  `id_diagnostico` bigint(20) UNSIGNED NOT NULL,
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `descripcion` varchar(500) NOT NULL,
  `es_principal` tinyint(1) NOT NULL DEFAULT 0,
  `observaciones` text DEFAULT NULL,
  `fecha_registro` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `consulta_diagnosticos`
--

INSERT INTO `consulta_diagnosticos` (`id_diagnostico`, `id_consulta`, `descripcion`, `es_principal`, `observaciones`, `fecha_registro`) VALUES
(221, 190, 'Paciente apto para inmunización preventiva.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(222, 191, 'Dermatitis alérgica leve.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(223, 192, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(224, 193, 'Paciente apto para desparasitación preventiva.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(225, 194, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(226, 195, 'Paciente apto para inmunización preventiva.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(227, 196, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(228, 197, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(229, 198, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(230, 199, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(231, 200, 'Paciente apto para inmunización preventiva.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(232, 201, 'Cojera en evaluación diagnóstica.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(233, 202, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(234, 203, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(235, 204, 'Paciente apto para inmunización preventiva.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(236, 205, 'Paciente apto para desparasitación preventiva.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37'),
(237, 206, 'Paciente clínicamente estable.', 1, 'Diagnóstico principal registrado durante consulta.', '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `consulta_servicios`
--

DROP TABLE IF EXISTS `consulta_servicios`;
CREATE TABLE `consulta_servicios` (
  `id_consulta_servicio` bigint(20) UNSIGNED NOT NULL,
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `id_servicio` int(10) UNSIGNED NOT NULL,
  `descripcion` varchar(250) NOT NULL,
  `cantidad` decimal(10,2) NOT NULL DEFAULT 1.00,
  `precio_unitario` decimal(12,2) NOT NULL DEFAULT 0.00,
  `descuento` decimal(12,2) NOT NULL DEFAULT 0.00,
  `subtotal` decimal(12,2) NOT NULL DEFAULT 0.00,
  `genera_cargo` tinyint(1) NOT NULL DEFAULT 1,
  `facturado` tinyint(1) NOT NULL DEFAULT 0,
  `fecha_registro` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `consulta_servicios`
--

INSERT INTO `consulta_servicios` (`id_consulta_servicio`, `id_consulta`, `id_servicio`, `descripcion`, `cantidad`, `precio_unitario`, `descuento`, `subtotal`, `genera_cargo`, `facturado`, `fecha_registro`) VALUES
(221, 190, 66, 'Vacunación', 1.00, 95.00, 0.00, 95.00, 1, 1, '2026-05-27 22:50:37'),
(222, 191, 64, 'Consulta dermatológica', 1.00, 340.00, 0.00, 340.00, 1, 1, '2026-05-27 22:50:37'),
(223, 192, 59, 'Consulta general', 1.00, 180.00, 0.00, 180.00, 1, 1, '2026-05-27 22:50:37'),
(224, 193, 67, 'Desparasitación', 1.00, 75.00, 0.00, 75.00, 1, 1, '2026-05-27 22:50:37'),
(225, 194, 60, 'Consulta especializada', 1.00, 300.00, 0.00, 300.00, 1, 1, '2026-05-27 22:50:37'),
(226, 195, 66, 'Vacunación', 1.00, 95.00, 0.00, 95.00, 1, 1, '2026-05-27 22:50:37'),
(227, 196, 68, 'Curación simple', 1.00, 135.00, 0.00, 135.00, 1, 1, '2026-05-27 22:50:37'),
(228, 197, 70, 'Toma de muestra', 1.00, 95.00, 0.00, 95.00, 1, 1, '2026-05-27 22:50:37'),
(229, 198, 59, 'Consulta general', 1.00, 180.00, 0.00, 180.00, 1, 1, '2026-05-27 22:50:37'),
(230, 199, 60, 'Consulta especializada', 1.00, 300.00, 0.00, 300.00, 1, 1, '2026-05-27 22:50:37'),
(231, 200, 66, 'Vacunación', 1.00, 95.00, 0.00, 95.00, 1, 1, '2026-05-27 22:50:37'),
(232, 201, 71, 'Radiografía', 1.00, 360.00, 0.00, 360.00, 1, 1, '2026-05-27 22:50:37'),
(233, 202, 59, 'Consulta general', 1.00, 180.00, 0.00, 180.00, 1, 1, '2026-05-27 22:50:37'),
(234, 203, 60, 'Consulta especializada', 1.00, 300.00, 0.00, 300.00, 1, 1, '2026-05-27 22:50:37'),
(235, 204, 66, 'Vacunación', 1.00, 95.00, 0.00, 95.00, 1, 1, '2026-05-27 22:50:37'),
(236, 205, 67, 'Desparasitación', 1.00, 75.00, 0.00, 75.00, 1, 1, '2026-05-27 22:50:37'),
(237, 206, 59, 'Consulta general', 1.00, 216.00, 0.00, 216.00, 1, 1, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `desparasitaciones`
--

DROP TABLE IF EXISTS `desparasitaciones`;
CREATE TABLE `desparasitaciones` (
  `id_desparasitacion` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `id_desparasitante` int(10) UNSIGNED NOT NULL,
  `id_lote_inventario` bigint(20) UNSIGNED DEFAULT NULL,
  `dosis` varchar(80) NOT NULL,
  `peso_referencia` decimal(8,2) DEFAULT NULL,
  `fecha_aplicacion` datetime NOT NULL,
  `fecha_proxima` date DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `precio_aplicado` decimal(12,2) NOT NULL DEFAULT 0.00,
  `facturado` tinyint(1) NOT NULL DEFAULT 0,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `desparasitaciones`
--

INSERT INTO `desparasitaciones` (`id_desparasitacion`, `id_mascota`, `id_consulta`, `id_desparasitante`, `id_lote_inventario`, `dosis`, `peso_referencia`, `fecha_aplicacion`, `fecha_proxima`, `observaciones`, `id_veterinario`, `precio_aplicado`, `facturado`, `fecha_creacion`) VALUES
(38, 291, 193, 20, 104, 'Según peso', 8.70, '2026-05-07 09:00:00', '2026-08-05', 'Desparasitación preventiva.', 2, 75.00, 1, '2026-05-27 22:50:37'),
(39, 303, 205, 20, 104, 'Según peso', 28.00, '2026-05-28 15:30:00', '2026-08-26', 'Desparasitación preventiva.', 2, 75.00, 1, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `duenos`
--

DROP TABLE IF EXISTS `duenos`;
CREATE TABLE `duenos` (
  `id_dueno` bigint(20) UNSIGNED NOT NULL,
  `codigo_cliente` varchar(20) NOT NULL,
  `nombre_completo` varchar(150) NOT NULL,
  `documento` varchar(50) DEFAULT NULL,
  `telefono_principal` varchar(30) NOT NULL,
  `telefono_alternativo` varchar(30) DEFAULT NULL,
  `correo` varchar(150) DEFAULT NULL,
  `direccion` varchar(250) DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `fecha_modificacion` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `duenos`
--

INSERT INTO `duenos` (`id_dueno`, `codigo_cliente`, `nombre_completo`, `documento`, `telefono_principal`, `telefono_alternativo`, `correo`, `direccion`, `observaciones`, `activo`, `fecha_creacion`, `fecha_modificacion`) VALUES
(211, 'CLI-000001', 'María González Paredes', 'DOC-000001', '555-1001', '555-2001', 'maria.gonzalez@email.com', 'Col. Jardines, Calle 3 #14', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(212, 'CLI-000002', 'Carlos Ramírez Soto', 'DOC-000002', '555-1002', NULL, 'carlos.ramirez@email.com', 'Col. Centro, Avenida Reforma #220', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(213, 'CLI-000003', 'Andrea Torres Medina', 'DOC-000003', '555-1003', '555-2003', 'andrea.torres@email.com', 'Residencial Las Flores #18', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(214, 'CLI-000004', 'Jorge Salazar Cruz', 'DOC-000004', '555-1004', NULL, 'jorge.salazar@email.com', 'Col. San Rafael, Calle Robles #9', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(215, 'CLI-000005', 'Patricia Hernández Ruiz', 'DOC-000005', '555-1005', '555-2005', 'patricia.hernandez@email.com', 'Col. Los Pinos, Calle Sur #31', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(216, 'CLI-000006', 'Miguel Ángel Navarro', 'DOC-000006', '555-1006', NULL, 'miguel.navarro@email.com', 'Fracc. El Mirador #42', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(217, 'CLI-000007', 'Valeria Castillo Rojas', 'DOC-000007', '555-1007', '555-2007', 'valeria.castillo@email.com', 'Col. Primavera, Calle 12 #5', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(218, 'CLI-000008', 'Daniel Ortega Fuentes', 'DOC-000008', '555-1008', NULL, 'daniel.ortega@email.com', 'Col. Centro, Calle Hidalgo #81', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(219, 'CLI-000009', 'Gabriela Herrera Luna', 'DOC-000009', '555-1009', '555-2009', 'gabriela.herrera@email.com', 'Col. Arboledas #102', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(220, 'CLI-000010', 'Ricardo Flores Aguilar', 'DOC-000010', '555-1010', NULL, 'ricardo.flores@email.com', 'Col. La Paz, Avenida Uno #53', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(221, 'CLI-000011', 'Sofía Vargas León', 'DOC-000011', '555-1011', '555-2011', 'sofia.vargas@email.com', 'Col. Vista Hermosa #22', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(222, 'CLI-000012', 'Héctor Jiménez Reyes', 'DOC-000012', '555-1012', NULL, 'hector.jimenez@email.com', 'Col. Bosques, Calle Encino #71', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(223, 'CLI-000013', 'Fernanda López Castro', 'DOC-000013', '555-1013', '555-2013', 'fernanda.lopez@email.com', 'Col. Alameda #118', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(224, 'CLI-000014', 'Eduardo Morales Peña', 'DOC-000014', '555-1014', NULL, 'eduardo.morales@email.com', 'Col. Reforma #67', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(225, 'CLI-000015', 'Camila Reyes Montoya', 'DOC-000015', '555-1015', '555-2015', 'camila.reyes@email.com', 'Col. del Valle #90', 'Cliente demo curado para validación funcional.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `facturas`
--

DROP TABLE IF EXISTS `facturas`;
CREATE TABLE `facturas` (
  `id_factura` bigint(20) UNSIGNED NOT NULL,
  `numero_factura` varchar(30) NOT NULL,
  `id_dueno` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED DEFAULT NULL,
  `id_cita` bigint(20) UNSIGNED DEFAULT NULL,
  `id_consulta` bigint(20) UNSIGNED DEFAULT NULL,
  `fecha_emision` datetime NOT NULL DEFAULT current_timestamp(),
  `subtotal` decimal(12,2) NOT NULL DEFAULT 0.00,
  `descuento_total` decimal(12,2) NOT NULL DEFAULT 0.00,
  `impuesto_total` decimal(12,2) NOT NULL DEFAULT 0.00,
  `total` decimal(12,2) NOT NULL DEFAULT 0.00,
  `total_pagado` decimal(12,2) NOT NULL DEFAULT 0.00,
  `saldo_pendiente` decimal(12,2) NOT NULL DEFAULT 0.00,
  `estado` enum('Borrador','Emitida','Parcialmente pagada','Pagada','Anulada') NOT NULL DEFAULT 'Borrador',
  `observaciones` text DEFAULT NULL,
  `id_usuario_creacion` int(10) UNSIGNED NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `id_usuario_anulacion` int(10) UNSIGNED DEFAULT NULL,
  `fecha_anulacion` datetime DEFAULT NULL,
  `motivo_anulacion` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `facturas`
--

INSERT INTO `facturas` (`id_factura`, `numero_factura`, `id_dueno`, `id_mascota`, `id_cita`, `id_consulta`, `fecha_emision`, `subtotal`, `descuento_total`, `impuesto_total`, `total`, `total_pagado`, `saldo_pendiente`, `estado`, `observaciones`, `id_usuario_creacion`, `fecha_creacion`, `id_usuario_anulacion`, `fecha_anulacion`, `motivo_anulacion`) VALUES
(190, 'FAC-2026-000190', 211, 288, 246, 190, '2026-05-04 09:00:00', 95.00, 0.00, 10.18, 95.00, 47.50, 47.50, 'Parcialmente pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(191, 'FAC-2026-000191', 212, 289, 247, 191, '2026-05-05 10:00:00', 340.00, 0.00, 36.43, 340.00, 340.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(192, 'FAC-2026-000192', 212, 290, 248, 192, '2026-05-06 11:30:00', 180.00, 0.00, 19.29, 180.00, 180.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(193, 'FAC-2026-000193', 213, 291, 249, 193, '2026-05-07 09:00:00', 75.00, 0.00, 8.04, 75.00, 75.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(194, 'FAC-2026-000194', 214, 292, 250, 194, '2026-05-11 09:30:00', 300.00, 0.00, 32.14, 300.00, 300.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(195, 'FAC-2026-000195', 215, 293, 251, 195, '2026-05-12 10:00:00', 95.00, 0.00, 10.18, 95.00, 47.50, 47.50, 'Parcialmente pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(196, 'FAC-2026-000196', 215, 294, 252, 196, '2026-05-13 15:00:00', 135.00, 0.00, 14.46, 135.00, 135.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(197, 'FAC-2026-000197', 216, 295, 253, 197, '2026-05-14 12:00:00', 95.00, 0.00, 10.18, 95.00, 95.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(198, 'FAC-2026-000198', 217, 296, 254, 198, '2026-05-18 08:30:00', 180.00, 0.00, 19.29, 180.00, 180.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(199, 'FAC-2026-000199', 217, 297, 255, 199, '2026-05-19 13:00:00', 300.00, 0.00, 32.14, 300.00, 300.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(200, 'FAC-2026-000200', 218, 298, 256, 200, '2026-05-20 09:00:00', 95.00, 0.00, 10.18, 95.00, 47.50, 47.50, 'Parcialmente pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(201, 'FAC-2026-000201', 219, 299, 257, 201, '2026-05-21 10:00:00', 360.00, 0.00, 38.57, 360.00, 360.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(202, 'FAC-2026-000202', 219, 300, 258, 202, '2026-05-25 09:00:00', 180.00, 0.00, 19.29, 180.00, 180.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(203, 'FAC-2026-000203', 220, 301, 259, 203, '2026-05-26 09:30:00', 300.00, 0.00, 32.14, 300.00, 300.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(204, 'FAC-2026-000204', 221, 302, 260, 204, '2026-05-27 11:00:00', 95.00, 0.00, 10.18, 95.00, 95.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(205, 'FAC-2026-000205', 222, 303, 261, 205, '2026-05-28 15:30:00', 75.00, 0.00, 8.04, 75.00, 37.50, 37.50, 'Parcialmente pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(206, 'FAC-2026-000206', 224, 307, 265, 206, '2026-05-08 16:00:00', 216.00, 0.00, 23.14, 216.00, 216.00, 0.00, 'Pagada', 'Factura histórica en quetzales. Precio total con IVA Guatemala incluido (12%).', 1, '2026-05-27 22:50:37', NULL, NULL, NULL),
(221, 'FAC-2026-000101', 224, 307, NULL, NULL, '2026-05-27 22:55:29', 180.00, 0.00, 19.29, 180.00, 0.00, 180.00, 'Emitida', NULL, 1, '2026-05-27 22:55:29', NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `factura_detalles`
--

DROP TABLE IF EXISTS `factura_detalles`;
CREATE TABLE `factura_detalles` (
  `id_detalle` bigint(20) UNSIGNED NOT NULL,
  `id_factura` bigint(20) UNSIGNED NOT NULL,
  `tipo_item` enum('Servicio','Medicamento','Vacuna','Desparasitante','Producto','Laboratorio','Hospitalización','Otro') NOT NULL,
  `id_referencia` bigint(20) UNSIGNED DEFAULT NULL,
  `descripcion` varchar(250) NOT NULL,
  `cantidad` decimal(10,2) NOT NULL,
  `precio_unitario` decimal(12,2) NOT NULL,
  `descuento` decimal(12,2) NOT NULL DEFAULT 0.00,
  `subtotal` decimal(12,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `factura_detalles`
--

INSERT INTO `factura_detalles` (`id_detalle`, `id_factura`, `tipo_item`, `id_referencia`, `descripcion`, `cantidad`, `precio_unitario`, `descuento`, `subtotal`) VALUES
(190, 190, 'Servicio', 221, 'Vacunación', 1.00, 95.00, 0.00, 95.00),
(191, 191, 'Servicio', 222, 'Consulta dermatológica', 1.00, 340.00, 0.00, 340.00),
(192, 192, 'Servicio', 223, 'Consulta general', 1.00, 180.00, 0.00, 180.00),
(193, 193, 'Servicio', 224, 'Desparasitación', 1.00, 75.00, 0.00, 75.00),
(194, 194, 'Servicio', 225, 'Consulta especializada', 1.00, 300.00, 0.00, 300.00),
(195, 195, 'Servicio', 226, 'Vacunación', 1.00, 95.00, 0.00, 95.00),
(196, 196, 'Servicio', 227, 'Curación simple', 1.00, 135.00, 0.00, 135.00),
(197, 197, 'Servicio', 228, 'Toma de muestra', 1.00, 95.00, 0.00, 95.00),
(198, 198, 'Servicio', 229, 'Consulta general', 1.00, 180.00, 0.00, 180.00),
(199, 199, 'Servicio', 230, 'Consulta especializada', 1.00, 300.00, 0.00, 300.00),
(200, 200, 'Servicio', 231, 'Vacunación', 1.00, 95.00, 0.00, 95.00),
(201, 201, 'Servicio', 232, 'Radiografía', 1.00, 360.00, 0.00, 360.00),
(202, 202, 'Servicio', 233, 'Consulta general', 1.00, 180.00, 0.00, 180.00),
(203, 203, 'Servicio', 234, 'Consulta especializada', 1.00, 300.00, 0.00, 300.00),
(204, 204, 'Servicio', 235, 'Vacunación', 1.00, 95.00, 0.00, 95.00),
(205, 205, 'Servicio', 236, 'Desparasitación', 1.00, 75.00, 0.00, 75.00),
(206, 206, 'Servicio', 237, 'Consulta general', 1.00, 216.00, 0.00, 216.00),
(221, 221, 'Servicio', NULL, 'Control preventivo pendiente de facturar', 1.00, 180.00, 0.00, 180.00);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `hospitalizaciones`
--

DROP TABLE IF EXISTS `hospitalizaciones`;
CREATE TABLE `hospitalizaciones` (
  `id_hospitalizacion` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `id_consulta_origen` bigint(20) UNSIGNED DEFAULT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `id_jaula` int(10) UNSIGNED DEFAULT NULL,
  `fecha_hora_ingreso` datetime NOT NULL,
  `fecha_hora_alta` datetime DEFAULT NULL,
  `motivo` varchar(500) NOT NULL,
  `espacio_asignado` varchar(100) DEFAULT NULL,
  `estado` enum('Ingresada','En observación','Alta','Cancelada') NOT NULL DEFAULT 'Ingresada',
  `observaciones` text DEFAULT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `hospitalizaciones`
--

INSERT INTO `hospitalizaciones` (`id_hospitalizacion`, `id_mascota`, `id_consulta_origen`, `id_veterinario`, `id_jaula`, `fecha_hora_ingreso`, `fecha_hora_alta`, `motivo`, `espacio_asignado`, `estado`, `observaciones`, `fecha_creacion`) VALUES
(22, 294, 196, 1, NULL, '2026-05-13 16:00:00', '2026-05-14 15:00:00', 'Observación clínica posterior a procedimiento.', 'JAU-000001', 'Alta', 'Paciente evolucionó favorablemente.', '2026-05-27 22:50:37'),
(23, 301, 203, 2, NULL, '2026-05-26 10:30:00', '2026-05-27 09:30:00', 'Observación clínica posterior a procedimiento.', 'JAU-000001', 'Alta', 'Paciente evolucionó favorablemente.', '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `hospitalizacion_evoluciones`
--

DROP TABLE IF EXISTS `hospitalizacion_evoluciones`;
CREATE TABLE `hospitalizacion_evoluciones` (
  `id_evolucion` bigint(20) UNSIGNED NOT NULL,
  `id_hospitalizacion` bigint(20) UNSIGNED NOT NULL,
  `fecha_hora` datetime NOT NULL DEFAULT current_timestamp(),
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `temperatura` decimal(5,2) DEFAULT NULL,
  `peso` decimal(8,2) DEFAULT NULL,
  `frecuencia_cardiaca` smallint(5) UNSIGNED DEFAULT NULL,
  `frecuencia_respiratoria` smallint(5) UNSIGNED DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  `medicacion_administrada` text DEFAULT NULL,
  `alimentacion` text DEFAULT NULL,
  `incidencias` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `hospitalizacion_evoluciones`
--

INSERT INTO `hospitalizacion_evoluciones` (`id_evolucion`, `id_hospitalizacion`, `fecha_hora`, `id_veterinario`, `temperatura`, `peso`, `frecuencia_cardiaca`, `frecuencia_respiratoria`, `observaciones`, `medicacion_administrada`, `alimentacion`, `incidencias`) VALUES
(29, 22, '2026-05-13 20:00:00', 1, 38.40, 13.40, 88, 22, 'Paciente estable durante observación.', 'Tratamiento indicado.', 'Alimentación tolerada.', 'Sin incidencias.'),
(30, 23, '2026-05-26 14:30:00', 2, 38.40, 39.20, 88, 22, 'Paciente estable durante observación.', 'Tratamiento indicado.', 'Alimentación tolerada.', 'Sin incidencias.');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inventario_lotes`
--

DROP TABLE IF EXISTS `inventario_lotes`;
CREATE TABLE `inventario_lotes` (
  `id_lote` bigint(20) UNSIGNED NOT NULL,
  `id_producto` bigint(20) UNSIGNED NOT NULL,
  `numero_lote` varchar(80) NOT NULL,
  `fecha_vencimiento` date DEFAULT NULL,
  `cantidad_inicial` decimal(12,3) NOT NULL,
  `cantidad_disponible` decimal(12,3) NOT NULL,
  `costo_unitario` decimal(12,2) NOT NULL DEFAULT 0.00,
  `fecha_ingreso` datetime NOT NULL DEFAULT current_timestamp(),
  `proveedor` varchar(150) DEFAULT NULL,
  `estado` enum('Disponible','Agotado','Vencido','Bloqueado') NOT NULL DEFAULT 'Disponible'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inventario_lotes`
--

INSERT INTO `inventario_lotes` (`id_lote`, `id_producto`, `numero_lote`, `fecha_vencimiento`, `cantidad_inicial`, `cantidad_disponible`, `costo_unitario`, `fecha_ingreso`, `proveedor`, `estado`) VALUES
(96, 59, 'MED-AMOX-2601', '2027-01-22', 30.000, 28.000, 65.00, '2026-05-27 22:50:37', 'Distribuidora Vet Salud', 'Disponible'),
(97, 60, 'MED-MELO-2601', '2026-12-23', 20.000, 18.000, 78.00, '2026-05-27 22:50:37', 'Distribuidora Vet Salud', 'Disponible'),
(98, 61, 'MED-CEFA-2601', '2026-11-23', 20.000, 20.000, 85.00, '2026-05-27 22:50:37', 'Farmacéutica Animal', 'Disponible'),
(99, 63, 'VAC-AR-2601', '2027-04-02', 40.000, 36.000, 95.00, '2026-05-27 22:50:37', 'Biológicos Veterinarios', 'Disponible'),
(100, 64, 'VAC-DHPP-2601', '2027-03-03', 35.000, 31.000, 122.00, '2026-05-27 22:50:37', 'Biológicos Veterinarios', 'Disponible'),
(101, 65, 'VAC-TRIF-2601', '2027-02-01', 25.000, 23.000, 130.00, '2026-05-27 22:50:37', 'Biológicos Veterinarios', 'Disponible'),
(102, 66, 'VAC-BORD-2601', '2027-01-12', 15.000, 15.000, 115.00, '2026-05-27 22:50:37', 'Biológicos Veterinarios', 'Disponible'),
(103, 67, 'VAC-FELV-2601', '2027-01-02', 15.000, 14.000, 145.00, '2026-05-27 22:50:37', 'Biológicos Veterinarios', 'Disponible'),
(104, 68, 'DES-ORA-2601', '2027-03-23', 60.000, 52.000, 30.00, '2026-05-27 22:50:37', 'Vet Pharma', 'Disponible'),
(105, 69, 'DES-SUS-2601', '2026-12-03', 25.000, 24.000, 48.00, '2026-05-27 22:50:37', 'Vet Pharma', 'Disponible'),
(106, 70, 'DES-PIP-2601', '2026-11-13', 35.000, 30.000, 55.00, '2026-05-27 22:50:37', 'Vet Pharma', 'Disponible'),
(107, 71, 'INS-JER-2601', '2027-10-09', 10.000, 8.000, 80.00, '2026-05-27 22:50:37', 'Materiales Médicos', 'Disponible'),
(108, 72, 'INS-GUA-2601', '2027-10-09', 12.000, 10.000, 95.00, '2026-05-27 22:50:37', 'Materiales Médicos', 'Disponible'),
(109, 73, 'ALI-REC-2601', '2026-06-21', 30.000, 14.000, 30.00, '2026-05-27 22:50:37', 'Nutrición Animal', 'Disponible');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inventario_movimientos`
--

DROP TABLE IF EXISTS `inventario_movimientos`;
CREATE TABLE `inventario_movimientos` (
  `id_movimiento` bigint(20) UNSIGNED NOT NULL,
  `id_producto` bigint(20) UNSIGNED NOT NULL,
  `id_lote` bigint(20) UNSIGNED DEFAULT NULL,
  `tipo_movimiento` enum('Entrada','Salida por consulta','Salida por venta','Salida por vacuna aplicada','Ajuste positivo','Ajuste negativo','Merma','Vencimiento','Movimiento compensatorio por anulación') NOT NULL,
  `cantidad` decimal(12,3) NOT NULL,
  `id_consulta` bigint(20) UNSIGNED DEFAULT NULL,
  `id_factura` bigint(20) UNSIGNED DEFAULT NULL,
  `id_vacuna_aplicada` bigint(20) UNSIGNED DEFAULT NULL,
  `id_usuario_registro` int(10) UNSIGNED NOT NULL,
  `fecha_registro` datetime NOT NULL DEFAULT current_timestamp(),
  `observaciones` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inventario_movimientos`
--

INSERT INTO `inventario_movimientos` (`id_movimiento`, `id_producto`, `id_lote`, `tipo_movimiento`, `cantidad`, `id_consulta`, `id_factura`, `id_vacuna_aplicada`, `id_usuario_registro`, `fecha_registro`, `observaciones`) VALUES
(156, 59, 96, 'Entrada', 30.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(157, 60, 97, 'Entrada', 20.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(158, 61, 98, 'Entrada', 20.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(159, 63, 99, 'Entrada', 40.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(160, 64, 100, 'Entrada', 35.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(161, 65, 101, 'Entrada', 25.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(162, 66, 102, 'Entrada', 15.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(163, 67, 103, 'Entrada', 15.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(164, 68, 104, 'Entrada', 60.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(165, 69, 105, 'Entrada', 25.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(166, 70, 106, 'Entrada', 35.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(167, 71, 107, 'Entrada', 10.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(168, 72, 108, 'Entrada', 12.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.'),
(169, 73, 109, 'Entrada', 30.000, NULL, NULL, NULL, 1, '2026-05-27 22:50:37', 'Existencia inicial de base demo curada.');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inventario_productos`
--

DROP TABLE IF EXISTS `inventario_productos`;
CREATE TABLE `inventario_productos` (
  `id_producto` bigint(20) UNSIGNED NOT NULL,
  `codigo` varchar(25) NOT NULL,
  `nombre` varchar(150) NOT NULL,
  `categoria` enum('Medicamento','Vacuna','Desparasitante','Insumo','Producto de venta') NOT NULL,
  `presentacion` varchar(120) DEFAULT NULL,
  `unidad_medida` varchar(40) NOT NULL,
  `precio_compra` decimal(12,2) NOT NULL DEFAULT 0.00,
  `precio_venta` decimal(12,2) NOT NULL DEFAULT 0.00,
  `stock_minimo` decimal(12,3) NOT NULL DEFAULT 0.000,
  `controla_lotes` tinyint(1) NOT NULL DEFAULT 1,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inventario_productos`
--

INSERT INTO `inventario_productos` (`id_producto`, `codigo`, `nombre`, `categoria`, `presentacion`, `unidad_medida`, `precio_compra`, `precio_venta`, `stock_minimo`, `controla_lotes`, `activo`) VALUES
(59, 'INV-MED-001', 'Amoxicilina veterinaria', 'Medicamento', 'Tabletas 250 mg', 'Caja', 65.00, 125.00, 4.000, 1, 1),
(60, 'INV-MED-002', 'Meloxicam veterinario', 'Medicamento', 'Suspensión 30 ml', 'Frasco', 78.00, 155.00, 3.000, 1, 1),
(61, 'INV-MED-003', 'Cefalexina veterinaria', 'Medicamento', 'Tabletas 500 mg', 'Caja', 85.00, 165.00, 3.000, 1, 1),
(62, 'INV-MED-004', 'Clorhexidina', 'Medicamento', 'Solución 250 ml', 'Frasco', 40.00, 90.00, 4.000, 1, 1),
(63, 'INV-VAC-001', 'Vacuna Antirrábica', 'Vacuna', 'Frasco monodosis', 'Dosis', 95.00, 180.00, 8.000, 1, 1),
(64, 'INV-VAC-002', 'Vacuna Múltiple Canina DHPP', 'Vacuna', 'Frasco monodosis', 'Dosis', 122.00, 230.00, 8.000, 1, 1),
(65, 'INV-VAC-003', 'Vacuna Triple Felina', 'Vacuna', 'Frasco monodosis', 'Dosis', 130.00, 245.00, 6.000, 1, 1),
(66, 'INV-VAC-004', 'Vacuna Bordetella', 'Vacuna', 'Frasco monodosis', 'Dosis', 115.00, 215.00, 4.000, 1, 1),
(67, 'INV-VAC-005', 'Vacuna Leucemia Felina', 'Vacuna', 'Frasco monodosis', 'Dosis', 145.00, 270.00, 4.000, 1, 1),
(68, 'INV-DES-001', 'Desparasitante oral amplio espectro', 'Desparasitante', 'Tableta', 'Unidad', 30.00, 75.00, 10.000, 1, 1),
(69, 'INV-DES-002', 'Desparasitante suspensión cachorro', 'Desparasitante', 'Frasco 30 ml', 'Frasco', 48.00, 110.00, 5.000, 1, 1),
(70, 'INV-DES-003', 'Pipeta antiparasitaria externa', 'Desparasitante', 'Pipeta', 'Unidad', 55.00, 120.00, 8.000, 1, 1),
(71, 'INV-INS-001', 'Jeringa 3 ml', 'Insumo', 'Caja 100 unidades', 'Caja', 80.00, 0.00, 2.000, 1, 1),
(72, 'INV-INS-002', 'Guantes de exploración', 'Insumo', 'Caja 100 unidades', 'Caja', 95.00, 135.00, 3.000, 1, 1),
(73, 'INV-VTA-001', 'Alimento recuperación', 'Producto de venta', 'Lata 150 g', 'Lata', 30.00, 58.00, 8.000, 1, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `mascotas`
--

DROP TABLE IF EXISTS `mascotas`;
CREATE TABLE `mascotas` (
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `codigo_paciente` varchar(20) NOT NULL,
  `id_dueno` bigint(20) UNSIGNED NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `especie` varchar(50) NOT NULL,
  `raza` varchar(80) DEFAULT NULL,
  `sexo` enum('Macho','Hembra','Desconocido') NOT NULL DEFAULT 'Desconocido',
  `color` varchar(80) DEFAULT NULL,
  `fecha_nacimiento` date DEFAULT NULL,
  `peso_actual` decimal(8,2) DEFAULT NULL,
  `esterilizado` tinyint(1) NOT NULL DEFAULT 0,
  `microchip` varchar(80) DEFAULT NULL,
  `ruta_foto` varchar(350) DEFAULT NULL,
  `estado_vital` enum('Viva','Fallecida','Inactiva') NOT NULL DEFAULT 'Viva',
  `fecha_fallecimiento` date DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `fecha_modificacion` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `mascotas`
--

INSERT INTO `mascotas` (`id_mascota`, `codigo_paciente`, `id_dueno`, `nombre`, `especie`, `raza`, `sexo`, `color`, `fecha_nacimiento`, `peso_actual`, `esterilizado`, `microchip`, `ruta_foto`, `estado_vital`, `fecha_fallecimiento`, `observaciones`, `activo`, `fecha_creacion`, `fecha_modificacion`) VALUES
(288, 'PAC-000001', 211, 'Luna', 'Canino', 'Labrador Retriever', 'Hembra', 'Dorado', '2021-04-15', 24.50, 1, 'CHIP-000001', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(289, 'PAC-000002', 212, 'Max', 'Canino', 'Pastor Alemán', 'Macho', 'Negro y café', '2020-09-08', 32.80, 0, 'CHIP-000002', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(290, 'PAC-000003', 212, 'Misha', 'Felino', 'Siamés', 'Hembra', 'Crema', '2023-02-10', 4.30, 1, 'CHIP-000003', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(291, 'PAC-000004', 213, 'Coco', 'Canino', 'Poodle', 'Macho', 'Blanco', '2022-11-02', 8.70, 1, 'CHIP-000004', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(292, 'PAC-000005', 214, 'Rocky', 'Canino', 'Bulldog Inglés', 'Macho', 'Café', '2019-06-12', 22.10, 0, 'CHIP-000005', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(293, 'PAC-000006', 215, 'Nala', 'Felino', 'Europeo', 'Hembra', 'Gris', '2022-03-07', 4.80, 1, 'CHIP-000006', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(294, 'PAC-000007', 215, 'Bruno', 'Canino', 'Beagle', 'Macho', 'Tricolor', '2021-08-19', 13.40, 0, 'CHIP-000007', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(295, 'PAC-000008', 216, 'Kira', 'Canino', 'Husky Siberiano', 'Hembra', 'Blanco y gris', '2020-12-01', 21.90, 1, 'CHIP-000008', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(296, 'PAC-000009', 217, 'Toby', 'Canino', 'Chihuahua', 'Macho', 'Miel', '2024-01-20', 3.20, 0, 'CHIP-000009', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(297, 'PAC-000010', 217, 'Canela', 'Felino', 'Persa', 'Hembra', 'Naranja', '2018-05-05', 5.10, 1, 'CHIP-000010', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(298, 'PAC-000011', 218, 'Simba', 'Felino', 'Mestizo', 'Macho', 'Atigrado', '2021-10-24', 5.80, 1, 'CHIP-000011', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(299, 'PAC-000012', 219, 'Milo', 'Canino', 'Golden Retriever', 'Macho', 'Dorado', '2022-06-10', 26.30, 0, 'CHIP-000012', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(300, 'PAC-000013', 219, 'Pelusa', 'Felino', 'Angora', 'Hembra', 'Blanco', '2020-02-14', 4.60, 1, 'CHIP-000013', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(301, 'PAC-000014', 220, 'Thor', 'Canino', 'Rottweiler', 'Macho', 'Negro', '2019-09-30', 39.20, 0, 'CHIP-000014', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(302, 'PAC-000015', 221, 'Maya', 'Canino', 'Border Collie', 'Hembra', 'Negro y blanco', '2023-04-18', 15.60, 1, 'CHIP-000015', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(303, 'PAC-000016', 222, 'Zeus', 'Canino', 'Boxer', 'Macho', 'Café', '2020-07-22', 28.00, 0, 'CHIP-000016', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(304, 'PAC-000017', 222, 'Bimba', 'Felino', 'Bengalí', 'Hembra', 'Manchado', '2022-09-09', 4.20, 1, 'CHIP-000017', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(305, 'PAC-000018', 223, 'Dante', 'Canino', 'Schnauzer', 'Macho', 'Sal y pimienta', '2021-01-13', 9.80, 1, 'CHIP-000018', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(306, 'PAC-000019', 224, 'Olivia', 'Felino', 'Maine Coon', 'Hembra', 'Gris', '2019-11-11', 6.30, 1, 'CHIP-000019', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(307, 'PAC-000020', 224, 'Kiwi', 'Ave', 'Ninfa', 'Hembra', 'Gris y amarillo', '2023-06-03', 0.12, 0, NULL, NULL, 'Viva', NULL, 'Ave de compañía; manejo delicado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(308, 'PAC-000021', 225, 'Rex', 'Canino', 'Dálmata', 'Macho', 'Blanco y negro', '2018-08-27', 25.50, 0, 'CHIP-000021', NULL, 'Viva', NULL, 'Paciente demo curado.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37'),
(309, 'PAC-000022', 225, 'Pixel', 'Conejo', 'Holland Lop', 'Hembra', 'Blanco y gris', '2023-07-16', 1.65, 1, NULL, NULL, 'Viva', NULL, 'Mascota exótica de compañía.', 1, '2026-05-27 22:50:37', '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `mascota_alertas_clinicas`
--

DROP TABLE IF EXISTS `mascota_alertas_clinicas`;
CREATE TABLE `mascota_alertas_clinicas` (
  `id_alerta` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `tipo_alerta` enum('Alergia','Condición crónica','Medicamento contraindicado','Conducta agresiva','Recomendación especial','Otra') NOT NULL,
  `descripcion` varchar(500) NOT NULL,
  `activa` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_registro` datetime NOT NULL DEFAULT current_timestamp(),
  `id_usuario_registro` int(10) UNSIGNED NOT NULL,
  `fecha_cierre` datetime DEFAULT NULL,
  `id_usuario_cierre` int(10) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `mascota_alertas_clinicas`
--

INSERT INTO `mascota_alertas_clinicas` (`id_alerta`, `id_mascota`, `tipo_alerta`, `descripcion`, `activa`, `fecha_registro`, `id_usuario_registro`, `fecha_cierre`, `id_usuario_cierre`) VALUES
(88, 289, 'Conducta agresiva', 'Utilizar bozal durante procedimientos y manipulación clínica.', 1, '2026-05-27 22:50:37', 1, NULL, NULL),
(89, 292, 'Condición crónica', 'Paciente con sobrepeso en seguimiento nutricional.', 1, '2026-05-27 22:50:37', 1, NULL, NULL),
(90, 288, 'Alergia', 'Reacción previa a penicilina; verificar medicamentos.', 1, '2026-05-27 22:50:37', 1, NULL, NULL),
(91, 297, 'Medicamento contraindicado', 'Evitar antiinflamatorios sin evaluación renal previa.', 1, '2026-05-27 22:50:37', 1, NULL, NULL),
(92, 301, 'Recomendación especial', 'Paciente grande; manipular con apoyo adicional.', 1, '2026-05-27 22:50:37', 1, NULL, NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `metodos_pago`
--

DROP TABLE IF EXISTS `metodos_pago`;
CREATE TABLE `metodos_pago` (
  `id_metodo_pago` int(10) UNSIGNED NOT NULL,
  `nombre` varchar(80) NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `metodos_pago`
--

INSERT INTO `metodos_pago` (`id_metodo_pago`, `nombre`, `activo`) VALUES
(21, 'Efectivo', 1),
(22, 'Tarjeta débito', 1),
(23, 'Tarjeta crédito', 1),
(24, 'Transferencia', 1),
(25, 'Depósito bancario', 1),
(26, 'Cheque', 1),
(27, 'Otro', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `ordenes_clinicas`
--

DROP TABLE IF EXISTS `ordenes_clinicas`;
CREATE TABLE `ordenes_clinicas` (
  `id_orden` bigint(20) UNSIGNED NOT NULL,
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `tipo_orden` enum('Laboratorio','Imagen','Otro estudio') NOT NULL,
  `nombre_estudio` varchar(150) NOT NULL,
  `motivo` varchar(500) DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  `estado` enum('Solicitada','En proceso','Resultado recibido','Cancelada') NOT NULL DEFAULT 'Solicitada',
  `precio` decimal(12,2) NOT NULL DEFAULT 0.00,
  `facturado` tinyint(1) NOT NULL DEFAULT 0,
  `fecha_solicitud` datetime NOT NULL DEFAULT current_timestamp(),
  `fecha_resultado` datetime DEFAULT NULL,
  `resultado_texto` text DEFAULT NULL,
  `ruta_archivo` varchar(350) DEFAULT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `ordenes_clinicas`
--

INSERT INTO `ordenes_clinicas` (`id_orden`, `id_consulta`, `tipo_orden`, `nombre_estudio`, `motivo`, `observaciones`, `estado`, `precio`, `facturado`, `fecha_solicitud`, `fecha_resultado`, `resultado_texto`, `ruta_archivo`, `id_veterinario`) VALUES
(38, 192, 'Laboratorio', 'Hemograma completo', 'Evaluación complementaria.', 'Solicitud histórica.', 'Resultado recibido', 240.00, 1, '2026-05-06 11:30:00', '2026-05-07 11:30:00', 'Resultados dentro de parámetros esperados.', NULL, 2),
(39, 196, 'Laboratorio', 'Hemograma completo', 'Evaluación complementaria.', 'Solicitud histórica.', 'Resultado recibido', 240.00, 1, '2026-05-13 15:00:00', '2026-05-14 15:00:00', 'Resultados dentro de parámetros esperados.', NULL, 1),
(40, 200, 'Laboratorio', 'Hemograma completo', 'Evaluación complementaria.', 'Solicitud histórica.', 'Resultado recibido', 240.00, 1, '2026-05-20 09:00:00', '2026-05-21 09:00:00', 'Resultados dentro de parámetros esperados.', NULL, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `pagos`
--

DROP TABLE IF EXISTS `pagos`;
CREATE TABLE `pagos` (
  `id_pago` bigint(20) UNSIGNED NOT NULL,
  `id_factura` bigint(20) UNSIGNED NOT NULL,
  `id_metodo_pago` int(10) UNSIGNED NOT NULL,
  `fecha_pago` datetime NOT NULL DEFAULT current_timestamp(),
  `monto` decimal(12,2) NOT NULL,
  `referencia` varchar(120) DEFAULT NULL,
  `observaciones` varchar(400) DEFAULT NULL,
  `id_usuario_registro` int(10) UNSIGNED NOT NULL,
  `estado` enum('Aplicado','Anulado') NOT NULL DEFAULT 'Aplicado',
  `id_usuario_anulacion` int(10) UNSIGNED DEFAULT NULL,
  `fecha_anulacion` datetime DEFAULT NULL,
  `motivo_anulacion` varchar(500) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `pagos`
--

INSERT INTO `pagos` (`id_pago`, `id_factura`, `id_metodo_pago`, `fecha_pago`, `monto`, `referencia`, `observaciones`, `id_usuario_registro`, `estado`, `id_usuario_anulacion`, `fecha_anulacion`, `motivo_anulacion`) VALUES
(109, 190, 21, '2026-05-04 09:00:00', 47.50, 'PAGO-000190', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(110, 191, 21, '2026-05-05 10:00:00', 340.00, 'PAGO-000191', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(111, 192, 21, '2026-05-06 11:30:00', 180.00, 'PAGO-000192', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(112, 193, 21, '2026-05-07 09:00:00', 75.00, 'PAGO-000193', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(113, 194, 21, '2026-05-11 09:30:00', 300.00, 'PAGO-000194', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(114, 195, 21, '2026-05-12 10:00:00', 47.50, 'PAGO-000195', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(115, 196, 21, '2026-05-13 15:00:00', 135.00, 'PAGO-000196', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(116, 197, 21, '2026-05-14 12:00:00', 95.00, 'PAGO-000197', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(117, 198, 21, '2026-05-18 08:30:00', 180.00, 'PAGO-000198', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(118, 199, 21, '2026-05-19 13:00:00', 300.00, 'PAGO-000199', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(119, 200, 21, '2026-05-20 09:00:00', 47.50, 'PAGO-000200', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(120, 201, 21, '2026-05-21 10:00:00', 360.00, 'PAGO-000201', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(121, 202, 21, '2026-05-25 09:00:00', 180.00, 'PAGO-000202', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(122, 203, 21, '2026-05-26 09:30:00', 300.00, 'PAGO-000203', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(123, 204, 21, '2026-05-27 11:00:00', 95.00, 'PAGO-000204', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(124, 205, 21, '2026-05-28 15:30:00', 37.50, 'PAGO-000205', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL),
(125, 206, 21, '2026-05-08 16:00:00', 216.00, 'PAGO-000206', 'Pago demo registrado.', 1, 'Aplicado', NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `recetas`
--

DROP TABLE IF EXISTS `recetas`;
CREATE TABLE `recetas` (
  `id_receta` bigint(20) UNSIGNED NOT NULL,
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `fecha_emision` datetime NOT NULL DEFAULT current_timestamp(),
  `indicaciones_generales` text DEFAULT NULL,
  `id_usuario_creacion` int(10) UNSIGNED NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `recetas`
--

INSERT INTO `recetas` (`id_receta`, `id_consulta`, `fecha_emision`, `indicaciones_generales`, `id_usuario_creacion`, `fecha_creacion`) VALUES
(78, 190, '2026-05-04 09:00:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37'),
(79, 192, '2026-05-06 11:30:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37'),
(80, 194, '2026-05-11 09:30:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37'),
(81, 196, '2026-05-13 15:00:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37'),
(82, 198, '2026-05-18 08:30:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37'),
(83, 200, '2026-05-20 09:00:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37'),
(84, 202, '2026-05-25 09:00:00', 'Administrar medicación según pauta y vigilar respuesta.', 1, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `receta_detalles`
--

DROP TABLE IF EXISTS `receta_detalles`;
CREATE TABLE `receta_detalles` (
  `id_detalle` bigint(20) UNSIGNED NOT NULL,
  `id_receta` bigint(20) UNSIGNED NOT NULL,
  `id_medicamento` int(10) UNSIGNED DEFAULT NULL,
  `medicamento_libre` varchar(150) DEFAULT NULL,
  `presentacion` varchar(120) DEFAULT NULL,
  `concentracion` varchar(80) DEFAULT NULL,
  `dosis` varchar(100) NOT NULL,
  `frecuencia` varchar(100) NOT NULL,
  `duracion` varchar(100) NOT NULL,
  `cantidad` varchar(80) DEFAULT NULL,
  `via_administracion` varchar(100) DEFAULT NULL,
  `indicaciones` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `receta_detalles`
--

INSERT INTO `receta_detalles` (`id_detalle`, `id_receta`, `id_medicamento`, `medicamento_libre`, `presentacion`, `concentracion`, `dosis`, `frecuencia`, `duracion`, `cantidad`, `via_administracion`, `indicaciones`) VALUES
(78, 78, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.'),
(79, 79, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.'),
(80, 80, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.'),
(81, 81, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.'),
(82, 82, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.'),
(83, 83, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.'),
(84, 84, 31, NULL, NULL, NULL, 'Según peso', 'Cada 24 horas', '3 días', '1 frasco', 'Oral', 'Administrar después del alimento.');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `recordatorios`
--

DROP TABLE IF EXISTS `recordatorios`;
CREATE TABLE `recordatorios` (
  `id_recordatorio` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `tipo_recordatorio` enum('Vacuna','Desparasitación','Revisión clínica','Cita por confirmar','Control posoperatorio','Manual') NOT NULL,
  `fecha_programada` date NOT NULL,
  `descripcion` varchar(500) NOT NULL,
  `estado` enum('Pendiente','Contactado','Pospuesto','Completado','Cancelado') NOT NULL DEFAULT 'Pendiente',
  `fecha_contacto` datetime DEFAULT NULL,
  `observaciones_contacto` varchar(500) DEFAULT NULL,
  `id_usuario_creacion` int(10) UNSIGNED NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `id_usuario_modificacion` int(10) UNSIGNED DEFAULT NULL,
  `fecha_modificacion` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `recordatorios`
--

INSERT INTO `recordatorios` (`id_recordatorio`, `id_mascota`, `tipo_recordatorio`, `fecha_programada`, `descripcion`, `estado`, `fecha_contacto`, `observaciones_contacto`, `id_usuario_creacion`, `fecha_creacion`, `id_usuario_modificacion`, `fecha_modificacion`) VALUES
(251, 288, 'Vacuna', '2026-06-06', 'Recordatorio de vacuna anual próxima.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(252, 293, 'Vacuna', '2026-06-06', 'Recordatorio de vacuna anual próxima.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(253, 298, 'Vacuna', '2026-06-06', 'Recordatorio de vacuna anual próxima.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(254, 291, 'Desparasitación', '2026-06-11', 'Control de desparasitación programado.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(255, 303, 'Desparasitación', '2026-06-11', 'Control de desparasitación programado.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(256, 289, 'Revisión clínica', '2026-06-01', 'Seguimiento clínico recomendado.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(257, 292, 'Revisión clínica', '2026-06-01', 'Seguimiento clínico recomendado.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37'),
(258, 299, 'Revisión clínica', '2026-06-01', 'Seguimiento clínico recomendado.', 'Pendiente', NULL, NULL, 1, '2026-05-27 22:50:37', NULL, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `roles`
--

DROP TABLE IF EXISTS `roles`;
CREATE TABLE `roles` (
  `id_rol` int(10) UNSIGNED NOT NULL,
  `nombre` varchar(50) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `roles`
--

INSERT INTO `roles` (`id_rol`, `nombre`, `descripcion`, `activo`) VALUES
(1, 'Administrador', 'Acceso administrativo completo y supervisión del sistema.', 1),
(2, 'Recepción', 'Gestión de clientes, pacientes y agenda.', 1),
(3, 'Veterinario', 'Atención clínica, expedientes y seguimiento médico.', 1),
(4, 'Caja', 'Facturación, pagos y control de caja.', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `secuencias_documentos`
--

DROP TABLE IF EXISTS `secuencias_documentos`;
CREATE TABLE `secuencias_documentos` (
  `id_secuencia` bigint(20) UNSIGNED NOT NULL,
  `tipo_documento` varchar(20) NOT NULL,
  `anio` smallint(5) UNSIGNED NOT NULL,
  `ultimo_numero` bigint(20) UNSIGNED NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `secuencias_documentos`
--

INSERT INTO `secuencias_documentos` (`id_secuencia`, `tipo_documento`, `anio`, `ultimo_numero`) VALUES
(4, 'FAC', 2026, 101);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `tipos_bloqueo`
--

DROP TABLE IF EXISTS `tipos_bloqueo`;
CREATE TABLE `tipos_bloqueo` (
  `id_tipo_bloqueo` int(10) UNSIGNED NOT NULL,
  `nombre` varchar(80) NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `tipos_bloqueo`
--

INSERT INTO `tipos_bloqueo` (`id_tipo_bloqueo`, `nombre`, `activo`) VALUES
(28, 'Almuerzo', 1),
(29, 'Vacaciones', 1),
(30, 'Incapacidad', 1),
(31, 'Capacitación', 1),
(32, 'Ausencia', 1),
(33, 'Cirugía programada', 1),
(34, 'Reunión clínica', 1),
(35, 'Reserva administrativa', 1),
(36, 'Otro', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `usuarios`
--

DROP TABLE IF EXISTS `usuarios`;
CREATE TABLE `usuarios` (
  `id_usuario` int(10) UNSIGNED NOT NULL,
  `id_rol` int(10) UNSIGNED NOT NULL,
  `nombre_usuario` varchar(60) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `nombre_completo` varchar(150) NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `ultimo_acceso` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `usuarios`
--

INSERT INTO `usuarios` (`id_usuario`, `id_rol`, `nombre_usuario`, `password_hash`, `nombre_completo`, `activo`, `fecha_creacion`, `ultimo_acceso`) VALUES
(1, 1, 'admin', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Administrador Inicial', 1, '2026-05-27 16:24:40', '2026-05-27 23:59:35'),
(2, 2, 'recepcion', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Recepción Demo', 1, '2026-05-27 17:27:57', '2026-05-28 00:40:20'),
(3, 3, 'vetdemo', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Veterinario Demo', 1, '2026-05-27 17:27:57', NULL),
(4, 2, 'recepcionprueba', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Recepción Prueba', 1, '2026-05-27 17:40:59', NULL),
(5, 3, 'vetagenda1', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Dra. Ana Morales', 1, '2026-05-27 17:40:59', '2026-05-28 00:00:02'),
(6, 3, 'vetagenda2', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Dr. Luis Herrera', 1, '2026-05-27 17:40:59', '2026-05-27 21:12:12'),
(7, 4, 'cajaprueba', 'PBKDF2-SHA256$100000$LFV9X+9fQmS2LIgWKSYDEA==$LIGLrDs0rb6xar0+PH2Mv5vWp6fsQo8bzjzcy6/KsK0=', 'Caja Prueba', 1, '2026-05-27 17:40:59', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `vacunas_aplicadas`
--

DROP TABLE IF EXISTS `vacunas_aplicadas`;
CREATE TABLE `vacunas_aplicadas` (
  `id_aplicacion` bigint(20) UNSIGNED NOT NULL,
  `id_mascota` bigint(20) UNSIGNED NOT NULL,
  `id_consulta` bigint(20) UNSIGNED NOT NULL,
  `id_vacuna` int(10) UNSIGNED NOT NULL,
  `id_lote_inventario` bigint(20) UNSIGNED DEFAULT NULL,
  `lote_texto` varchar(80) DEFAULT NULL,
  `laboratorio` varchar(120) DEFAULT NULL,
  `fecha_vencimiento_lote` date DEFAULT NULL,
  `dosis` varchar(80) NOT NULL,
  `fecha_aplicacion` datetime NOT NULL,
  `fecha_proxima_dosis` date DEFAULT NULL,
  `observaciones` text DEFAULT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `precio_aplicado` decimal(12,2) NOT NULL DEFAULT 0.00,
  `facturado` tinyint(1) NOT NULL DEFAULT 0,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `vacunas_aplicadas`
--

INSERT INTO `vacunas_aplicadas` (`id_aplicacion`, `id_mascota`, `id_consulta`, `id_vacuna`, `id_lote_inventario`, `lote_texto`, `laboratorio`, `fecha_vencimiento_lote`, `dosis`, `fecha_aplicacion`, `fecha_proxima_dosis`, `observaciones`, `id_veterinario`, `precio_aplicado`, `facturado`, `fecha_creacion`) VALUES
(46, 288, 190, 26, 99, 'VAC-AR-2601', 'Biológicos Veterinarios', '2027-02-01', '1 dosis', '2026-05-04 09:00:00', '2027-05-04', 'Aplicación preventiva registrada.', 1, 220.00, 1, '2026-05-27 22:50:37'),
(47, 293, 195, 28, 101, 'VAC-TRIF-2601', 'Biológicos Veterinarios', '2027-02-01', '1 dosis', '2026-05-12 10:00:00', '2027-05-12', 'Aplicación preventiva registrada.', 2, 220.00, 1, '2026-05-27 22:50:37'),
(48, 298, 200, 28, 101, 'VAC-TRIF-2601', 'Biológicos Veterinarios', '2027-02-01', '1 dosis', '2026-05-20 09:00:00', '2027-05-20', 'Aplicación preventiva registrada.', 1, 220.00, 1, '2026-05-27 22:50:37'),
(49, 302, 204, 26, 99, 'VAC-AR-2601', 'Biológicos Veterinarios', '2027-02-01', '1 dosis', '2026-05-27 11:00:00', '2027-05-27', 'Aplicación preventiva registrada.', 1, 220.00, 1, '2026-05-27 22:50:37');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `veterinarios`
--

DROP TABLE IF EXISTS `veterinarios`;
CREATE TABLE `veterinarios` (
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `codigo_veterinario` varchar(20) NOT NULL,
  `id_usuario` int(10) UNSIGNED DEFAULT NULL,
  `nombre_completo` varchar(150) NOT NULL,
  `numero_profesional` varchar(80) DEFAULT NULL,
  `especialidad` varchar(100) DEFAULT NULL,
  `telefono` varchar(30) DEFAULT NULL,
  `correo` varchar(150) DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `veterinarios`
--

INSERT INTO `veterinarios` (`id_veterinario`, `codigo_veterinario`, `id_usuario`, `nombre_completo`, `numero_profesional`, `especialidad`, `telefono`, `correo`, `activo`, `fecha_creacion`) VALUES
(1, 'VET-AGENDA-001', 5, 'Dra. Ana Morales', 'PROF-ANA-001', 'Medicina general', '555-5001', 'ana.morales@clinica.com', 1, '2026-05-27 17:40:59'),
(2, 'VET-AGENDA-002', 6, 'Dr. Luis Herrera', 'PROF-LUIS-001', 'Dermatología veterinaria', '555-5002', 'luis.herrera@clinica.com', 1, '2026-05-27 17:40:59');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `veterinario_bloqueos`
--

DROP TABLE IF EXISTS `veterinario_bloqueos`;
CREATE TABLE `veterinario_bloqueos` (
  `id_bloqueo` bigint(20) UNSIGNED NOT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `id_tipo_bloqueo` int(10) UNSIGNED NOT NULL,
  `fecha_hora_inicio` datetime NOT NULL,
  `fecha_hora_fin` datetime NOT NULL,
  `motivo` varchar(300) NOT NULL,
  `estado` enum('Vigente','Cancelado') NOT NULL DEFAULT 'Vigente',
  `id_usuario_creacion` int(10) UNSIGNED NOT NULL,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `id_usuario_cancelacion` int(10) UNSIGNED DEFAULT NULL,
  `fecha_cancelacion` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `veterinario_horarios`
--

DROP TABLE IF EXISTS `veterinario_horarios`;
CREATE TABLE `veterinario_horarios` (
  `id_horario` bigint(20) UNSIGNED NOT NULL,
  `id_veterinario` int(10) UNSIGNED NOT NULL,
  `dia_semana` tinyint(3) UNSIGNED NOT NULL COMMENT '1=Lunes ... 7=Domingo',
  `hora_inicio` time NOT NULL,
  `hora_fin` time NOT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `veterinario_horarios`
--

INSERT INTO `veterinario_horarios` (`id_horario`, `id_veterinario`, `dia_semana`, `hora_inicio`, `hora_fin`, `activo`, `fecha_creacion`) VALUES
(46, 1, 1, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(47, 2, 1, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(48, 1, 2, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(49, 2, 2, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(50, 1, 3, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(51, 2, 3, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(52, 1, 4, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(53, 2, 4, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(54, 1, 5, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37'),
(55, 2, 5, '08:00:00', '18:00:00', 1, '2026-05-27 22:50:37');

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `cargos_pendientes`
--
ALTER TABLE `cargos_pendientes`
  ADD PRIMARY KEY (`id_cargo`),
  ADD KEY `fk_cargos_dueno` (`id_dueno`),
  ADD KEY `fk_cargos_mascota` (`id_mascota`),
  ADD KEY `fk_cargos_consulta` (`id_consulta`),
  ADD KEY `fk_cargos_cita` (`id_cita`),
  ADD KEY `fk_cargos_factura` (`id_factura`),
  ADD KEY `ix_cargos_estado` (`estado`,`fecha_creacion`);

--
-- Indices de la tabla `catalogo_desparasitantes`
--
ALTER TABLE `catalogo_desparasitantes`
  ADD PRIMARY KEY (`id_desparasitante`),
  ADD UNIQUE KEY `uq_desparasitantes_codigo` (`codigo`),
  ADD KEY `fk_desparasitante_producto` (`id_producto_inventario`);

--
-- Indices de la tabla `catalogo_jaulas`
--
ALTER TABLE `catalogo_jaulas`
  ADD PRIMARY KEY (`id_jaula`),
  ADD UNIQUE KEY `uq_jaulas_codigo` (`codigo_jaula`),
  ADD UNIQUE KEY `uq_jaulas_nombre` (`nombre`);

--
-- Indices de la tabla `catalogo_medicamentos`
--
ALTER TABLE `catalogo_medicamentos`
  ADD PRIMARY KEY (`id_medicamento`),
  ADD UNIQUE KEY `uq_medicamentos_codigo` (`codigo`),
  ADD KEY `fk_medicamento_producto` (`id_producto_inventario`);

--
-- Indices de la tabla `catalogo_servicios`
--
ALTER TABLE `catalogo_servicios`
  ADD PRIMARY KEY (`id_servicio`),
  ADD UNIQUE KEY `uq_servicios_codigo` (`codigo`);

--
-- Indices de la tabla `catalogo_vacunas`
--
ALTER TABLE `catalogo_vacunas`
  ADD PRIMARY KEY (`id_vacuna`),
  ADD UNIQUE KEY `uq_vacunas_codigo` (`codigo`),
  ADD KEY `fk_vacuna_producto` (`id_producto_inventario`);

--
-- Indices de la tabla `citas`
--
ALTER TABLE `citas`
  ADD PRIMARY KEY (`id_cita`),
  ADD KEY `fk_citas_servicio` (`id_servicio`),
  ADD KEY `fk_citas_usr_crea` (`id_usuario_creacion`),
  ADD KEY `fk_citas_usr_mod` (`id_usuario_modificacion`),
  ADD KEY `ix_citas_fecha_vet` (`fecha_hora_inicio`,`id_veterinario`,`estado`),
  ADD KEY `ix_citas_vet_fecha` (`id_veterinario`,`fecha_hora_inicio`,`estado`),
  ADD KEY `ix_citas_mascota_fecha` (`id_mascota`,`fecha_hora_inicio`);

--
-- Indices de la tabla `cita_bloques`
--
ALTER TABLE `cita_bloques`
  ADD PRIMARY KEY (`id_bloque`),
  ADD UNIQUE KEY `uq_cita_bloque_veterinario` (`id_veterinario`,`fecha_hora_bloque`),
  ADD KEY `ix_cita_bloques_cita` (`id_cita`);

--
-- Indices de la tabla `cita_historial_estados`
--
ALTER TABLE `cita_historial_estados`
  ADD PRIMARY KEY (`id_historial`),
  ADD KEY `fk_historial_cita` (`id_cita`),
  ADD KEY `fk_historial_usuario` (`id_usuario`);

--
-- Indices de la tabla `cita_reagendamientos`
--
ALTER TABLE `cita_reagendamientos`
  ADD PRIMARY KEY (`id_reagendamiento`),
  ADD KEY `fk_reagenda_cita` (`id_cita`),
  ADD KEY `fk_reagenda_vet_ant` (`id_veterinario_anterior`),
  ADD KEY `fk_reagenda_vet_nvo` (`id_veterinario_nuevo`),
  ADD KEY `fk_reagenda_usuario` (`id_usuario`);

--
-- Indices de la tabla `consultas`
--
ALTER TABLE `consultas`
  ADD PRIMARY KEY (`id_consulta`),
  ADD UNIQUE KEY `uq_consultas_cita` (`id_cita`),
  ADD KEY `fk_consultas_veterinario` (`id_veterinario`),
  ADD KEY `fk_consultas_usuario` (`id_usuario_creacion`),
  ADD KEY `ix_consultas_mascota_fecha` (`id_mascota`,`fecha_atencion`);

--
-- Indices de la tabla `consulta_diagnosticos`
--
ALTER TABLE `consulta_diagnosticos`
  ADD PRIMARY KEY (`id_diagnostico`),
  ADD KEY `fk_diagnosticos_consulta` (`id_consulta`);

--
-- Indices de la tabla `consulta_servicios`
--
ALTER TABLE `consulta_servicios`
  ADD PRIMARY KEY (`id_consulta_servicio`),
  ADD KEY `fk_conserv_consulta` (`id_consulta`),
  ADD KEY `fk_conserv_servicio` (`id_servicio`),
  ADD KEY `ix_conserv_pendiente` (`facturado`,`id_consulta`);

--
-- Indices de la tabla `desparasitaciones`
--
ALTER TABLE `desparasitaciones`
  ADD PRIMARY KEY (`id_desparasitacion`),
  ADD KEY `fk_desparas_consulta` (`id_consulta`),
  ADD KEY `fk_desparas_catalogo` (`id_desparasitante`),
  ADD KEY `fk_desparas_lote` (`id_lote_inventario`),
  ADD KEY `fk_desparas_vet` (`id_veterinario`),
  ADD KEY `ix_desparas_mascota_fecha` (`id_mascota`,`fecha_aplicacion`);

--
-- Indices de la tabla `duenos`
--
ALTER TABLE `duenos`
  ADD PRIMARY KEY (`id_dueno`),
  ADD UNIQUE KEY `uq_duenos_codigo` (`codigo_cliente`),
  ADD UNIQUE KEY `uq_duenos_documento` (`documento`),
  ADD KEY `ix_duenos_busqueda_nombre` (`nombre_completo`),
  ADD KEY `ix_duenos_busqueda_telefono` (`telefono_principal`);

--
-- Indices de la tabla `facturas`
--
ALTER TABLE `facturas`
  ADD PRIMARY KEY (`id_factura`),
  ADD UNIQUE KEY `uq_facturas_numero` (`numero_factura`),
  ADD KEY `fk_facturas_mascota` (`id_mascota`),
  ADD KEY `fk_facturas_cita` (`id_cita`),
  ADD KEY `fk_facturas_consulta` (`id_consulta`),
  ADD KEY `fk_facturas_usr_crea` (`id_usuario_creacion`),
  ADD KEY `fk_facturas_usr_anula` (`id_usuario_anulacion`),
  ADD KEY `ix_facturas_estado_fecha` (`estado`,`fecha_emision`),
  ADD KEY `ix_facturas_dueno` (`id_dueno`,`estado`);

--
-- Indices de la tabla `factura_detalles`
--
ALTER TABLE `factura_detalles`
  ADD PRIMARY KEY (`id_detalle`),
  ADD KEY `fk_factdet_factura` (`id_factura`);

--
-- Indices de la tabla `hospitalizaciones`
--
ALTER TABLE `hospitalizaciones`
  ADD PRIMARY KEY (`id_hospitalizacion`),
  ADD KEY `fk_hosp_mascota` (`id_mascota`),
  ADD KEY `fk_hosp_consulta` (`id_consulta_origen`),
  ADD KEY `fk_hosp_veterinario` (`id_veterinario`),
  ADD KEY `ix_hosp_estado` (`estado`,`fecha_hora_ingreso`),
  ADD KEY `fk_hosp_jaula` (`id_jaula`);

--
-- Indices de la tabla `hospitalizacion_evoluciones`
--
ALTER TABLE `hospitalizacion_evoluciones`
  ADD PRIMARY KEY (`id_evolucion`),
  ADD KEY `fk_evol_hospitalizacion` (`id_hospitalizacion`),
  ADD KEY `fk_evol_veterinario` (`id_veterinario`);

--
-- Indices de la tabla `inventario_lotes`
--
ALTER TABLE `inventario_lotes`
  ADD PRIMARY KEY (`id_lote`),
  ADD UNIQUE KEY `uq_lotes_producto_lote` (`id_producto`,`numero_lote`),
  ADD KEY `ix_lotes_vencimiento_stock` (`fecha_vencimiento`,`cantidad_disponible`,`estado`);

--
-- Indices de la tabla `inventario_movimientos`
--
ALTER TABLE `inventario_movimientos`
  ADD PRIMARY KEY (`id_movimiento`),
  ADD KEY `fk_mov_producto` (`id_producto`),
  ADD KEY `fk_mov_lote` (`id_lote`),
  ADD KEY `fk_mov_consulta` (`id_consulta`),
  ADD KEY `fk_mov_factura` (`id_factura`),
  ADD KEY `fk_mov_vacuna` (`id_vacuna_aplicada`),
  ADD KEY `fk_mov_usuario` (`id_usuario_registro`),
  ADD KEY `ix_movimientos_fecha` (`fecha_registro`,`tipo_movimiento`);

--
-- Indices de la tabla `inventario_productos`
--
ALTER TABLE `inventario_productos`
  ADD PRIMARY KEY (`id_producto`),
  ADD UNIQUE KEY `uq_inv_producto_codigo` (`codigo`);

--
-- Indices de la tabla `mascotas`
--
ALTER TABLE `mascotas`
  ADD PRIMARY KEY (`id_mascota`),
  ADD UNIQUE KEY `uq_mascotas_codigo` (`codigo_paciente`),
  ADD UNIQUE KEY `uq_mascotas_microchip` (`microchip`),
  ADD KEY `ix_mascotas_dueno` (`id_dueno`,`activo`),
  ADD KEY `ix_mascotas_busqueda_nombre` (`nombre`);

--
-- Indices de la tabla `mascota_alertas_clinicas`
--
ALTER TABLE `mascota_alertas_clinicas`
  ADD PRIMARY KEY (`id_alerta`),
  ADD KEY `fk_alertas_usuario_reg` (`id_usuario_registro`),
  ADD KEY `fk_alertas_usuario_cierre` (`id_usuario_cierre`),
  ADD KEY `ix_alertas_mascota_activas` (`id_mascota`,`activa`);

--
-- Indices de la tabla `metodos_pago`
--
ALTER TABLE `metodos_pago`
  ADD PRIMARY KEY (`id_metodo_pago`),
  ADD UNIQUE KEY `uq_metodos_pago_nombre` (`nombre`);

--
-- Indices de la tabla `ordenes_clinicas`
--
ALTER TABLE `ordenes_clinicas`
  ADD PRIMARY KEY (`id_orden`),
  ADD KEY `fk_orden_consulta` (`id_consulta`),
  ADD KEY `fk_orden_veterinario` (`id_veterinario`),
  ADD KEY `ix_ordenes_estado` (`estado`,`fecha_solicitud`);

--
-- Indices de la tabla `pagos`
--
ALTER TABLE `pagos`
  ADD PRIMARY KEY (`id_pago`),
  ADD KEY `fk_pagos_factura` (`id_factura`),
  ADD KEY `fk_pagos_metodo` (`id_metodo_pago`),
  ADD KEY `fk_pagos_usr_reg` (`id_usuario_registro`),
  ADD KEY `fk_pagos_usr_anula` (`id_usuario_anulacion`),
  ADD KEY `ix_pagos_fecha_estado` (`fecha_pago`,`estado`);

--
-- Indices de la tabla `recetas`
--
ALTER TABLE `recetas`
  ADD PRIMARY KEY (`id_receta`),
  ADD KEY `fk_recetas_consulta` (`id_consulta`),
  ADD KEY `fk_recetas_usuario` (`id_usuario_creacion`);

--
-- Indices de la tabla `receta_detalles`
--
ALTER TABLE `receta_detalles`
  ADD PRIMARY KEY (`id_detalle`),
  ADD KEY `fk_receta_det_receta` (`id_receta`),
  ADD KEY `fk_receta_det_medicamento` (`id_medicamento`);

--
-- Indices de la tabla `recordatorios`
--
ALTER TABLE `recordatorios`
  ADD PRIMARY KEY (`id_recordatorio`),
  ADD KEY `fk_recordatorios_mascota` (`id_mascota`),
  ADD KEY `fk_recordatorios_usr_crea` (`id_usuario_creacion`),
  ADD KEY `fk_recordatorios_usr_mod` (`id_usuario_modificacion`),
  ADD KEY `ix_recordatorios_fecha_estado` (`fecha_programada`,`estado`);

--
-- Indices de la tabla `roles`
--
ALTER TABLE `roles`
  ADD PRIMARY KEY (`id_rol`),
  ADD UNIQUE KEY `uq_roles_nombre` (`nombre`);

--
-- Indices de la tabla `secuencias_documentos`
--
ALTER TABLE `secuencias_documentos`
  ADD PRIMARY KEY (`id_secuencia`),
  ADD UNIQUE KEY `uq_secuencias_tipo_anio` (`tipo_documento`,`anio`);

--
-- Indices de la tabla `tipos_bloqueo`
--
ALTER TABLE `tipos_bloqueo`
  ADD PRIMARY KEY (`id_tipo_bloqueo`),
  ADD UNIQUE KEY `uq_tipos_bloqueo_nombre` (`nombre`);

--
-- Indices de la tabla `usuarios`
--
ALTER TABLE `usuarios`
  ADD PRIMARY KEY (`id_usuario`),
  ADD UNIQUE KEY `uq_usuarios_nombre` (`nombre_usuario`),
  ADD KEY `fk_usuarios_roles` (`id_rol`);

--
-- Indices de la tabla `vacunas_aplicadas`
--
ALTER TABLE `vacunas_aplicadas`
  ADD PRIMARY KEY (`id_aplicacion`),
  ADD KEY `fk_vac_ap_consulta` (`id_consulta`),
  ADD KEY `fk_vac_ap_vacuna` (`id_vacuna`),
  ADD KEY `fk_vac_ap_lote` (`id_lote_inventario`),
  ADD KEY `fk_vac_ap_vet` (`id_veterinario`),
  ADD KEY `ix_vacunas_mascota_fecha` (`id_mascota`,`fecha_aplicacion`);

--
-- Indices de la tabla `veterinarios`
--
ALTER TABLE `veterinarios`
  ADD PRIMARY KEY (`id_veterinario`),
  ADD UNIQUE KEY `uq_veterinarios_codigo` (`codigo_veterinario`),
  ADD UNIQUE KEY `uq_veterinarios_usuario` (`id_usuario`);

--
-- Indices de la tabla `veterinario_bloqueos`
--
ALTER TABLE `veterinario_bloqueos`
  ADD PRIMARY KEY (`id_bloqueo`),
  ADD KEY `fk_bloqueos_tipo` (`id_tipo_bloqueo`),
  ADD KEY `fk_bloqueos_usr_crea` (`id_usuario_creacion`),
  ADD KEY `fk_bloqueos_usr_canc` (`id_usuario_cancelacion`),
  ADD KEY `ix_bloqueos_vet_intervalo` (`id_veterinario`,`estado`,`fecha_hora_inicio`,`fecha_hora_fin`);

--
-- Indices de la tabla `veterinario_horarios`
--
ALTER TABLE `veterinario_horarios`
  ADD PRIMARY KEY (`id_horario`),
  ADD KEY `ix_horarios_vet_dia` (`id_veterinario`,`dia_semana`,`activo`);

--
-- AUTO_INCREMENT de las tablas volcadas
--

--
-- AUTO_INCREMENT de la tabla `cargos_pendientes`
--
ALTER TABLE `cargos_pendientes`
  MODIFY `id_cargo` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=372;

--
-- AUTO_INCREMENT de la tabla `catalogo_desparasitantes`
--
ALTER TABLE `catalogo_desparasitantes`
  MODIFY `id_desparasitante` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=28;

--
-- AUTO_INCREMENT de la tabla `catalogo_jaulas`
--
ALTER TABLE `catalogo_jaulas`
  MODIFY `id_jaula` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=35;

--
-- AUTO_INCREMENT de la tabla `catalogo_medicamentos`
--
ALTER TABLE `catalogo_medicamentos`
  MODIFY `id_medicamento` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=40;

--
-- AUTO_INCREMENT de la tabla `catalogo_servicios`
--
ALTER TABLE `catalogo_servicios`
  MODIFY `id_servicio` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=79;

--
-- AUTO_INCREMENT de la tabla `catalogo_vacunas`
--
ALTER TABLE `catalogo_vacunas`
  MODIFY `id_vacuna` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=36;

--
-- AUTO_INCREMENT de la tabla `citas`
--
ALTER TABLE `citas`
  MODIFY `id_cita` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=278;

--
-- AUTO_INCREMENT de la tabla `cita_bloques`
--
ALTER TABLE `cita_bloques`
  MODIFY `id_bloque` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=86;

--
-- AUTO_INCREMENT de la tabla `cita_historial_estados`
--
ALTER TABLE `cita_historial_estados`
  MODIFY `id_historial` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=394;

--
-- AUTO_INCREMENT de la tabla `cita_reagendamientos`
--
ALTER TABLE `cita_reagendamientos`
  MODIFY `id_reagendamiento` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT de la tabla `consultas`
--
ALTER TABLE `consultas`
  MODIFY `id_consulta` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=207;

--
-- AUTO_INCREMENT de la tabla `consulta_diagnosticos`
--
ALTER TABLE `consulta_diagnosticos`
  MODIFY `id_diagnostico` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=238;

--
-- AUTO_INCREMENT de la tabla `consulta_servicios`
--
ALTER TABLE `consulta_servicios`
  MODIFY `id_consulta_servicio` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=238;

--
-- AUTO_INCREMENT de la tabla `desparasitaciones`
--
ALTER TABLE `desparasitaciones`
  MODIFY `id_desparasitacion` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=40;

--
-- AUTO_INCREMENT de la tabla `duenos`
--
ALTER TABLE `duenos`
  MODIFY `id_dueno` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=226;

--
-- AUTO_INCREMENT de la tabla `facturas`
--
ALTER TABLE `facturas`
  MODIFY `id_factura` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=222;

--
-- AUTO_INCREMENT de la tabla `factura_detalles`
--
ALTER TABLE `factura_detalles`
  MODIFY `id_detalle` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=222;

--
-- AUTO_INCREMENT de la tabla `hospitalizaciones`
--
ALTER TABLE `hospitalizaciones`
  MODIFY `id_hospitalizacion` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=24;

--
-- AUTO_INCREMENT de la tabla `hospitalizacion_evoluciones`
--
ALTER TABLE `hospitalizacion_evoluciones`
  MODIFY `id_evolucion` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=31;

--
-- AUTO_INCREMENT de la tabla `inventario_lotes`
--
ALTER TABLE `inventario_lotes`
  MODIFY `id_lote` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=110;

--
-- AUTO_INCREMENT de la tabla `inventario_movimientos`
--
ALTER TABLE `inventario_movimientos`
  MODIFY `id_movimiento` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=170;

--
-- AUTO_INCREMENT de la tabla `inventario_productos`
--
ALTER TABLE `inventario_productos`
  MODIFY `id_producto` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=74;

--
-- AUTO_INCREMENT de la tabla `mascotas`
--
ALTER TABLE `mascotas`
  MODIFY `id_mascota` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=310;

--
-- AUTO_INCREMENT de la tabla `mascota_alertas_clinicas`
--
ALTER TABLE `mascota_alertas_clinicas`
  MODIFY `id_alerta` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=93;

--
-- AUTO_INCREMENT de la tabla `metodos_pago`
--
ALTER TABLE `metodos_pago`
  MODIFY `id_metodo_pago` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=28;

--
-- AUTO_INCREMENT de la tabla `ordenes_clinicas`
--
ALTER TABLE `ordenes_clinicas`
  MODIFY `id_orden` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=41;

--
-- AUTO_INCREMENT de la tabla `pagos`
--
ALTER TABLE `pagos`
  MODIFY `id_pago` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=126;

--
-- AUTO_INCREMENT de la tabla `recetas`
--
ALTER TABLE `recetas`
  MODIFY `id_receta` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=85;

--
-- AUTO_INCREMENT de la tabla `receta_detalles`
--
ALTER TABLE `receta_detalles`
  MODIFY `id_detalle` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=85;

--
-- AUTO_INCREMENT de la tabla `recordatorios`
--
ALTER TABLE `recordatorios`
  MODIFY `id_recordatorio` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=259;

--
-- AUTO_INCREMENT de la tabla `roles`
--
ALTER TABLE `roles`
  MODIFY `id_rol` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT de la tabla `secuencias_documentos`
--
ALTER TABLE `secuencias_documentos`
  MODIFY `id_secuencia` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT de la tabla `tipos_bloqueo`
--
ALTER TABLE `tipos_bloqueo`
  MODIFY `id_tipo_bloqueo` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=37;

--
-- AUTO_INCREMENT de la tabla `usuarios`
--
ALTER TABLE `usuarios`
  MODIFY `id_usuario` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT de la tabla `vacunas_aplicadas`
--
ALTER TABLE `vacunas_aplicadas`
  MODIFY `id_aplicacion` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=50;

--
-- AUTO_INCREMENT de la tabla `veterinarios`
--
ALTER TABLE `veterinarios`
  MODIFY `id_veterinario` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `veterinario_bloqueos`
--
ALTER TABLE `veterinario_bloqueos`
  MODIFY `id_bloqueo` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `veterinario_horarios`
--
ALTER TABLE `veterinario_horarios`
  MODIFY `id_horario` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=56;

--
-- Restricciones para tablas volcadas
--

--
-- Filtros para la tabla `cargos_pendientes`
--
ALTER TABLE `cargos_pendientes`
  ADD CONSTRAINT `fk_cargos_cita` FOREIGN KEY (`id_cita`) REFERENCES `citas` (`id_cita`),
  ADD CONSTRAINT `fk_cargos_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_cargos_dueno` FOREIGN KEY (`id_dueno`) REFERENCES `duenos` (`id_dueno`),
  ADD CONSTRAINT `fk_cargos_factura` FOREIGN KEY (`id_factura`) REFERENCES `facturas` (`id_factura`),
  ADD CONSTRAINT `fk_cargos_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`);

--
-- Filtros para la tabla `catalogo_desparasitantes`
--
ALTER TABLE `catalogo_desparasitantes`
  ADD CONSTRAINT `fk_desparasitante_producto` FOREIGN KEY (`id_producto_inventario`) REFERENCES `inventario_productos` (`id_producto`);

--
-- Filtros para la tabla `catalogo_medicamentos`
--
ALTER TABLE `catalogo_medicamentos`
  ADD CONSTRAINT `fk_medicamento_producto` FOREIGN KEY (`id_producto_inventario`) REFERENCES `inventario_productos` (`id_producto`);

--
-- Filtros para la tabla `catalogo_vacunas`
--
ALTER TABLE `catalogo_vacunas`
  ADD CONSTRAINT `fk_vacuna_producto` FOREIGN KEY (`id_producto_inventario`) REFERENCES `inventario_productos` (`id_producto`);

--
-- Filtros para la tabla `citas`
--
ALTER TABLE `citas`
  ADD CONSTRAINT `fk_citas_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_citas_servicio` FOREIGN KEY (`id_servicio`) REFERENCES `catalogo_servicios` (`id_servicio`),
  ADD CONSTRAINT `fk_citas_usr_crea` FOREIGN KEY (`id_usuario_creacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_citas_usr_mod` FOREIGN KEY (`id_usuario_modificacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_citas_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `cita_bloques`
--
ALTER TABLE `cita_bloques`
  ADD CONSTRAINT `fk_citab_cita` FOREIGN KEY (`id_cita`) REFERENCES `citas` (`id_cita`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_citab_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `cita_historial_estados`
--
ALTER TABLE `cita_historial_estados`
  ADD CONSTRAINT `fk_historial_cita` FOREIGN KEY (`id_cita`) REFERENCES `citas` (`id_cita`),
  ADD CONSTRAINT `fk_historial_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `cita_reagendamientos`
--
ALTER TABLE `cita_reagendamientos`
  ADD CONSTRAINT `fk_reagenda_cita` FOREIGN KEY (`id_cita`) REFERENCES `citas` (`id_cita`),
  ADD CONSTRAINT `fk_reagenda_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_reagenda_vet_ant` FOREIGN KEY (`id_veterinario_anterior`) REFERENCES `veterinarios` (`id_veterinario`),
  ADD CONSTRAINT `fk_reagenda_vet_nvo` FOREIGN KEY (`id_veterinario_nuevo`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `consultas`
--
ALTER TABLE `consultas`
  ADD CONSTRAINT `fk_consultas_cita` FOREIGN KEY (`id_cita`) REFERENCES `citas` (`id_cita`),
  ADD CONSTRAINT `fk_consultas_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_consultas_usuario` FOREIGN KEY (`id_usuario_creacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_consultas_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `consulta_diagnosticos`
--
ALTER TABLE `consulta_diagnosticos`
  ADD CONSTRAINT `fk_diagnosticos_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`);

--
-- Filtros para la tabla `consulta_servicios`
--
ALTER TABLE `consulta_servicios`
  ADD CONSTRAINT `fk_conserv_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_conserv_servicio` FOREIGN KEY (`id_servicio`) REFERENCES `catalogo_servicios` (`id_servicio`);

--
-- Filtros para la tabla `desparasitaciones`
--
ALTER TABLE `desparasitaciones`
  ADD CONSTRAINT `fk_desparas_catalogo` FOREIGN KEY (`id_desparasitante`) REFERENCES `catalogo_desparasitantes` (`id_desparasitante`),
  ADD CONSTRAINT `fk_desparas_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_desparas_lote` FOREIGN KEY (`id_lote_inventario`) REFERENCES `inventario_lotes` (`id_lote`),
  ADD CONSTRAINT `fk_desparas_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_desparas_vet` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `facturas`
--
ALTER TABLE `facturas`
  ADD CONSTRAINT `fk_facturas_cita` FOREIGN KEY (`id_cita`) REFERENCES `citas` (`id_cita`),
  ADD CONSTRAINT `fk_facturas_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_facturas_dueno` FOREIGN KEY (`id_dueno`) REFERENCES `duenos` (`id_dueno`),
  ADD CONSTRAINT `fk_facturas_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_facturas_usr_anula` FOREIGN KEY (`id_usuario_anulacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_facturas_usr_crea` FOREIGN KEY (`id_usuario_creacion`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `factura_detalles`
--
ALTER TABLE `factura_detalles`
  ADD CONSTRAINT `fk_factdet_factura` FOREIGN KEY (`id_factura`) REFERENCES `facturas` (`id_factura`);

--
-- Filtros para la tabla `hospitalizaciones`
--
ALTER TABLE `hospitalizaciones`
  ADD CONSTRAINT `fk_hosp_consulta` FOREIGN KEY (`id_consulta_origen`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_hosp_jaula` FOREIGN KEY (`id_jaula`) REFERENCES `catalogo_jaulas` (`id_jaula`),
  ADD CONSTRAINT `fk_hosp_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_hosp_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `hospitalizacion_evoluciones`
--
ALTER TABLE `hospitalizacion_evoluciones`
  ADD CONSTRAINT `fk_evol_hospitalizacion` FOREIGN KEY (`id_hospitalizacion`) REFERENCES `hospitalizaciones` (`id_hospitalizacion`),
  ADD CONSTRAINT `fk_evol_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `inventario_lotes`
--
ALTER TABLE `inventario_lotes`
  ADD CONSTRAINT `fk_lotes_producto` FOREIGN KEY (`id_producto`) REFERENCES `inventario_productos` (`id_producto`);

--
-- Filtros para la tabla `inventario_movimientos`
--
ALTER TABLE `inventario_movimientos`
  ADD CONSTRAINT `fk_mov_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_mov_factura` FOREIGN KEY (`id_factura`) REFERENCES `facturas` (`id_factura`),
  ADD CONSTRAINT `fk_mov_lote` FOREIGN KEY (`id_lote`) REFERENCES `inventario_lotes` (`id_lote`),
  ADD CONSTRAINT `fk_mov_producto` FOREIGN KEY (`id_producto`) REFERENCES `inventario_productos` (`id_producto`),
  ADD CONSTRAINT `fk_mov_usuario` FOREIGN KEY (`id_usuario_registro`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_mov_vacuna` FOREIGN KEY (`id_vacuna_aplicada`) REFERENCES `vacunas_aplicadas` (`id_aplicacion`);

--
-- Filtros para la tabla `mascotas`
--
ALTER TABLE `mascotas`
  ADD CONSTRAINT `fk_mascotas_duenos` FOREIGN KEY (`id_dueno`) REFERENCES `duenos` (`id_dueno`);

--
-- Filtros para la tabla `mascota_alertas_clinicas`
--
ALTER TABLE `mascota_alertas_clinicas`
  ADD CONSTRAINT `fk_alertas_mascotas` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_alertas_usuario_cierre` FOREIGN KEY (`id_usuario_cierre`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_alertas_usuario_reg` FOREIGN KEY (`id_usuario_registro`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `ordenes_clinicas`
--
ALTER TABLE `ordenes_clinicas`
  ADD CONSTRAINT `fk_orden_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_orden_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `pagos`
--
ALTER TABLE `pagos`
  ADD CONSTRAINT `fk_pagos_factura` FOREIGN KEY (`id_factura`) REFERENCES `facturas` (`id_factura`),
  ADD CONSTRAINT `fk_pagos_metodo` FOREIGN KEY (`id_metodo_pago`) REFERENCES `metodos_pago` (`id_metodo_pago`),
  ADD CONSTRAINT `fk_pagos_usr_anula` FOREIGN KEY (`id_usuario_anulacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_pagos_usr_reg` FOREIGN KEY (`id_usuario_registro`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `recetas`
--
ALTER TABLE `recetas`
  ADD CONSTRAINT `fk_recetas_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_recetas_usuario` FOREIGN KEY (`id_usuario_creacion`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `receta_detalles`
--
ALTER TABLE `receta_detalles`
  ADD CONSTRAINT `fk_receta_det_medicamento` FOREIGN KEY (`id_medicamento`) REFERENCES `catalogo_medicamentos` (`id_medicamento`),
  ADD CONSTRAINT `fk_receta_det_receta` FOREIGN KEY (`id_receta`) REFERENCES `recetas` (`id_receta`);

--
-- Filtros para la tabla `recordatorios`
--
ALTER TABLE `recordatorios`
  ADD CONSTRAINT `fk_recordatorios_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_recordatorios_usr_crea` FOREIGN KEY (`id_usuario_creacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_recordatorios_usr_mod` FOREIGN KEY (`id_usuario_modificacion`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `usuarios`
--
ALTER TABLE `usuarios`
  ADD CONSTRAINT `fk_usuarios_roles` FOREIGN KEY (`id_rol`) REFERENCES `roles` (`id_rol`);

--
-- Filtros para la tabla `vacunas_aplicadas`
--
ALTER TABLE `vacunas_aplicadas`
  ADD CONSTRAINT `fk_vac_ap_consulta` FOREIGN KEY (`id_consulta`) REFERENCES `consultas` (`id_consulta`),
  ADD CONSTRAINT `fk_vac_ap_lote` FOREIGN KEY (`id_lote_inventario`) REFERENCES `inventario_lotes` (`id_lote`),
  ADD CONSTRAINT `fk_vac_ap_mascota` FOREIGN KEY (`id_mascota`) REFERENCES `mascotas` (`id_mascota`),
  ADD CONSTRAINT `fk_vac_ap_vacuna` FOREIGN KEY (`id_vacuna`) REFERENCES `catalogo_vacunas` (`id_vacuna`),
  ADD CONSTRAINT `fk_vac_ap_vet` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `veterinarios`
--
ALTER TABLE `veterinarios`
  ADD CONSTRAINT `fk_veterinarios_usuario` FOREIGN KEY (`id_usuario`) REFERENCES `usuarios` (`id_usuario`);

--
-- Filtros para la tabla `veterinario_bloqueos`
--
ALTER TABLE `veterinario_bloqueos`
  ADD CONSTRAINT `fk_bloqueos_tipo` FOREIGN KEY (`id_tipo_bloqueo`) REFERENCES `tipos_bloqueo` (`id_tipo_bloqueo`),
  ADD CONSTRAINT `fk_bloqueos_usr_canc` FOREIGN KEY (`id_usuario_cancelacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_bloqueos_usr_crea` FOREIGN KEY (`id_usuario_creacion`) REFERENCES `usuarios` (`id_usuario`),
  ADD CONSTRAINT `fk_bloqueos_veterinario` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);

--
-- Filtros para la tabla `veterinario_horarios`
--
ALTER TABLE `veterinario_horarios`
  ADD CONSTRAINT `fk_horarios_veterinarios` FOREIGN KEY (`id_veterinario`) REFERENCES `veterinarios` (`id_veterinario`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
