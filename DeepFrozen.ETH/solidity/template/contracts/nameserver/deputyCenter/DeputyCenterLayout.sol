// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLayout.sol";

import "./DeputyCenterType.sol";

abstract contract DeputyCenterLayout is OwnableLayout, NameServiceRefLayout {

    //RO for reverse offset
    uint256 constant REAL_CALLDATA_LENGTH_FROM_RO = 350;
    uint256 constant REAL_CALLDATA_LENGTH_TO_RO = 318;

    uint256 constant TO_ADDRESS_FROM_RO = 318;
    uint256 constant TO_ADDRESS_TO_RO = 298;

    uint256 constant CHAIN_ID_FROM_RO = 298;
    uint256 constant CHAIN_ID_TO_RO = 266;

    uint256 constant BEFORE_FROM_RO = 266;
    uint256 constant BEFORE_TO_RO = 234;

    uint256 constant UNIQUE_NONCE_FROM_RO = 234;
    uint256 constant UNIQUE_NONCE_TO_RO = 202;

    uint256 constant DEPEND_UNIQUE_NONCE_FROM_RO = 202;
    uint256 constant DEPEND_UNIQUE_NONCE_TO_RO = 170;

    uint256 constant BARRIER_NONCE_FROM_RO = 170;
    uint256 constant BARRIER_NONCE_TO_RO = 138;

    uint256 constant VALUE_FROM_RO = 138;
    uint256 constant VALUE_UNIQUE_NONCE_TO_RO = 106;

    uint256 constant ONLY_DESIGNATED_TX_ORIGINAL_FROM_RO = 106;
    uint256 constant ONLY_DESIGNATED_TX_ORIGINAL_TO_RO = 105;

    uint256 constant DESIGNATED_SENDER_FROM_RO = 105;
    uint256 constant DESIGNATED_SENDER_TO_RO = 85;

    uint256 constant SIGNER_FROM_RO = 85;
    uint256 constant SIGNER_TO_RO = 65;

    uint256 constant R_FROM_RO = 65;
    uint256 constant R_TO_RO = 33;

    uint256 constant S_FROM_RO = 33;
    uint256 constant S_TO_RO = 1;

    uint256 constant V_FROM_RO = 1;
    uint256 constant V_TO_RO = 0;


    bool internal _entryMark;
    address internal _caller;
    DeputyCenterType.BarrierRecord[] internal _barrierRecords;
    mapping(address => mapping(uint256 => bool)) _uniqueNonces;


}
