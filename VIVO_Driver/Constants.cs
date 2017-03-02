using System;

namespace VIVO_Driver
{
        public enum Commands : UInt16
        {
            Ping = 0x1801,
            PingResp = 0x18FF,
            Pass = 0x2C01,
            PassResp = 0x2CFF,
            Poll = 0x2C02,
            Auth = 0x2C06,
            Read = 0x2C07,
            Write = 0x2C08,
        }

        public enum Status : byte
        {
            OK = 0x00,
            Incorrect_Header_Tag = 0x01,
            Unknown_Command = 0x02,
            Unknown_Sub_Command = 0x03,
            CRC_Error_in_Frame = 0x04,
            Incorrect_Parameter = 0x05,
            Parameter_Not_Supported = 0x06,
            Mal_formatted_Data = 0x07,
            Timeout = 0x08,
            Failed_NACK = 0x0A,
            Command_not_Allowed = 0x0B,
            Sub_Command_not_Allowed = 0x0C,
            Buffer_Overflow_Data_Length_too_large_for_reader_buffer = 0x0D,
            User_Interface_Event = 0x0E,
            Communication_type_not_supported,
            _VT_1,
            _burst,
            _etc = 0x11,
            Secure_interface_is_not_functional_or_is_in_an_intermediate_state = 0x12,
            Data_field_is_not_mod_8 = 0x13,
            Pad_0x80_not_found_where_expected = 0x14,
            Specified_key_type_is_invalid = 0x15,
            Could_not_retrieve_key_from_the_SAM_InitSecureComm = 0x16,
            ash_code_problem = 0x17,
            Could_not_store_the_key_into_the_SAM_InstallKey = 0x18,
            Frame_is_too_large = 0x19,
            Unit_powered_up_in_authentication_state_but_POS_must_resend_the_InitSecureComm_command = 0x1A,
            The_EEPROM_may_not_be_initialized_because_SecCommInterface_does_not_make_sense = 0x1B,
            Problem_encoding_APDU_Module_Specific_Status_Codes1 = 0x1C,
            Unsupported_Index_ILM_SAM_Transceiver_error_problem_communicating_with_the_SAM_Key_Mgr = 0x20,

            Unexpected_Sequence_Counter_in_multiple_frames_for_single_bitmap_ILM_Length_error_in_data_returned_from_the_SAM_Key_Mgr
                = 0x21,
            Improper_bit_map_ILM = 0x22,
            Request_Online_Authorization = 0x23,
            ViVOCard3_raw_data_read_successful = 0x24,
            Message_index_not_available_ILM_ViVOcomm_activate_transaction_card_type_ViVOcomm = 0x25,
            Version_Information_Mismatch_ILM = 0x26,
            Not_sending_commands_in_correct_index_message_index_ILM = 0x27,
            Time_out_or_next_expected_message_not_received_ILM = 0x28,
            ILM_languages_not_available_for_viewing_ILM = 0x29,
            Other_language_not_supported_ILM = 0x2A,
            Module_specific_errors_for_Key_Manager_Unknown_Error_from_SAM = 0x41,
            Module_specific_errors_for_Key_Manager_Invalid_data_detected_by_SAM = 0x42,
            Module_specific_errors_for_Key_Manager_Incomplete_data_detected_by_SAM = 0x43,
            Module_specific_errors_for_Key_Manager_Reserved = 0x44,
            Module_specific_errors_for_Key_Manager_Invalid_key_hash_algorithm = 0x45,
            Module_specific_errors_for_Key_Manager_Invalid_key_encryption_algorithm = 0x46,
            Module_specific_errors_for_Key_Manager_Invalid_modulus_length = 0x47,
            Module_specific_errors_for_Key_Manager_Invalid_exponent = 0x48,
            Module_specific_errors_for_Key_Manager_Key_already_exists = 0x49,
            Module_specific_errors_for_Key_Manager_No_space_for_new_RID = 0x4A,
            Module_specific_errors_for_Key_Manager_Key_not_found = 0x4B,
            Module_specific_errors_for_Key_Manager_Crypto_not_responding = 0x4C,
            Module_specific_errors_for_Key_Manager_Crypto_communication_error = 0x4D,
            Module_specific_errors_for_Key_Manager_All_key_slots_are_full_maximum_number_of_keys_has_been_installed = 0x4F,
            Auto_Switch_OK = 0x50,
            Auto_Switch_failed = 0x51,
            Command_Not_Sended = 0xFD,
            Unknown_Exception = 0xFE,
            Empty = 0xFF,
        }

        public enum CardType : byte
        {
            None_Card_Not_Detected_or_Could_not_Activate = 0x0,
            ISO_14443_Type_A_Supports_ISO_14443_4_Protocol = 0x1,
            ISO_14443_Type_B_Supports_ISO_14443_4_Protocol = 0x2,
            Mifare_Type_A_Standard = 0x3,
            Mifare_Type_A_Ultralight = 0x4,
            ISO_14443_Type_A_Does_not_support_ISO_14443_4_Protocol = 0x5,
            ISO_14443_Type_B_Does_not_support_ISO_14443_4_Protocol = 0x6,
            ISO_14443_Type_A_and_Mifare_NFC_phone = 0x7,
        }

        public enum KeyType : byte
        {
            KeyA = 0x1,
            KeyB = 0x2,
        }
}
