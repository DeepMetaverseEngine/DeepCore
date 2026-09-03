// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";

import "./AssetVaultEvent.sol";

interface AssetVaultInterface1 is NameServiceRefInterface, AssetVaultEvent {

    function depositErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) external;

    function depositErc20s(
        bytes32[] memory erc20TokenNames,
        address owner,
        uint256[] memory amounts
    ) external;

    function withdrawErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount,
        uint256 traceId
    ) external;

    function withdrawErc20s(
        bytes32[] memory erc20TokenNames,
        address owner,
        uint256[] memory amounts,
        uint256 traceId
    ) external;

}
