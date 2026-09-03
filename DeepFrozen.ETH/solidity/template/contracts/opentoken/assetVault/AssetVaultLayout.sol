// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/nameServiceRef/NameServiceRefLayout.sol";

abstract contract AssetVaultLayout is NameServiceRefLayout {

    //erc20Token => strategy
    mapping(bytes32 => uint256) internal _erc20DepositStrategy;
    mapping(bytes32 => address) internal _erc20DepositStrategyAddress;
    mapping(bytes32 => uint256) internal _erc20WithdrawStrategy;
    mapping(bytes32 => address) internal _erc20WithdrawStrategyAddress;

    //erc1155Token => strategy
    mapping(bytes32 => uint256) internal _erc1155DepositStrategy;
    mapping(bytes32 => address) internal _erc1155DepositStrategyAddress;
    mapping(bytes32 => uint256) internal _erc1155WithdrawStrategy;
    mapping(bytes32 => address) internal _erc1155WithdrawStrategyAddress;
}
