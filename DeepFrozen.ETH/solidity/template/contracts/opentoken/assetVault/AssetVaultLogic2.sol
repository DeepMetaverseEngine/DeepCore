// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../holders/HERC721HolderLogic.sol";
import "../holders/HERC1155HolderLogic.sol";
import "./AssetVaultInterface.sol";

import "../erc20improved/HERC20IMInterface.sol";
import "../erc1155improved/HERC1155IMInterface.sol";
import "../erc721improved/HERC721IMInterface.sol";

import "./AssetVaultType.sol";

abstract contract AssetVaultLogic2 is AssetVaultLayout, NameServiceRefLogic, HERC1155HolderLogic, AssetVaultInterface2 {

    using SafeMath for uint256;
    using SafeERC20 for IERC20;

    function _depositErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) internal {
        bytes32[] memory erc1155TokenNames = new bytes32[](1);
        erc1155TokenNames[0] = erc1155TokenName;
        uint256[] memory tokenIds = new uint256[](1);
        tokenIds[0] = tokenId;
        uint256[] memory amounts = new uint256[](1);
        amounts[0] = amount;

        _depositErc1155s(erc1155TokenNames, owner, tokenIds, amounts);
    }

    function _depositErc1155s(
        bytes32[] memory erc1155TokenNames,
        address owner,
        uint256[] memory tokenIds,
        uint256[] memory amounts
    ) internal {
        require(erc1155TokenNames.length == tokenIds.length && tokenIds.length == amounts.length, "_depositErc1155s, length mismatch");

        for (uint256 i = 0; i < erc1155TokenNames.length; i++) {

            bytes32 erc1155TokenName = erc1155TokenNames[i];
            address erc1155Token = ns().getSingleSafe(erc1155TokenName);

            uint256 tokenId = tokenIds[i];
            uint256 amount = amounts[i];

            uint256 depositStrategy = _erc1155DepositStrategy[erc1155TokenName];
            if (depositStrategy == AssetVaultType.ERC1155_DEPOSIT_STRATEGY_BURN) {

                HERC1155IMInterface(erc1155Token).burnSudo(owner, tokenId, amount);

            } else if (depositStrategy == AssetVaultType.ERC1155_DEPOSIT_STRATEGY_TRANSFER) {

                address holder = _erc1155DepositStrategyAddress[erc1155TokenName];
                uint256 temp = IERC1155(erc1155Token).balanceOf(holder, tokenId);
                IERC1155(erc1155Token).safeTransferFrom(owner, holder, tokenId, amount, "");
                amount = IERC1155(erc1155Token).balanceOf(holder, tokenId).sub(temp);

            } else if (depositStrategy == AssetVaultType.ERC1155_DEPOSIT_STRATEGY_STORE) {

                uint256 temp = IERC1155(erc1155Token).balanceOf(address(this), tokenId);
                IERC1155(erc1155Token).safeTransferFrom(owner, address(this), tokenId, amount, "");
                amount = IERC1155(erc1155Token).balanceOf(address(this), tokenId).sub(temp);

            } else {
                revert("unsupported token");
            }
        }
        emit DepositErc1155(owner, erc1155TokenNames, tokenIds, amounts, owner);
    }

    function _withdrawErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount,
        uint256 traceId
    ) internal {
        bytes32[] memory erc1155TokenNames = new bytes32[](1);
        erc1155TokenNames[0] = erc1155TokenName;
        uint256[] memory tokenIds = new uint256[](1);
        tokenIds[0] = tokenId;
        uint256[] memory amounts = new uint256[](1);
        amounts[0] = amount;

        _withdrawErc1155s(erc1155TokenNames, owner, tokenIds, amounts, traceId);
    }

    function _withdrawErc1155s(
        bytes32[] memory erc1155TokenNames,
        address owner,
        uint256[] memory tokenIds,
        uint256[] memory amounts,
        uint256 traceId
    ) internal {
        require(erc1155TokenNames.length == tokenIds.length && tokenIds.length == amounts.length, "_depositErc1155s, length mismatch");

        for (uint256 i = 0; i < erc1155TokenNames.length; i++) {

            bytes32 erc1155TokenName = erc1155TokenNames[i];
            address erc1155Token = ns().getSingleSafe(erc1155TokenName);

            uint256 tokenId = tokenIds[i];

            uint256 amount = _withdraw1155Hook(erc1155TokenName, owner, tokenId, amounts[i]);
            amounts[i] = amount;

            uint256 withdrawStrategy = _erc1155WithdrawStrategy[erc1155TokenName];
            if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_MINT) {
                HERC1155IMInterface(erc1155Token).mintSudo(owner, tokenId, amount);
            } else if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_TRANSFER) {
                address holder = _erc1155WithdrawStrategyAddress[erc1155TokenName];
                IERC1155(erc1155Token).safeTransferFrom(holder, owner, tokenId, amount, "");
            } else if (
                withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_MINT ||
                withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_REVERT ||
                withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_TRANSFER
            ) {
                uint256 bal = IERC1155(erc1155Token).balanceOf(address(this), tokenId);
                if (bal < amount) {

                    uint256 gap = amount.sub(bal);

                    if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_MINT) {
                        //mint the remaining
                        HERC1155IMInterface(erc1155Token).mintSudo(owner, tokenId, gap);
                    } else if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_REVERT) {
                        revert("insufficient erc1155 token for withdraw");
                    } else if (withdrawStrategy == AssetVaultType.ERC1155_WITHDRAW_STRATEGY_RESTORE_TRANSFER) {
                        //the address must be approve first
                        address holder = _erc1155WithdrawStrategyAddress[erc1155TokenName];
                        IERC1155(erc1155Token).safeTransferFrom(holder, owner, tokenId, gap, "");
                    } else {
                        revert("withdrawErc1155 unknown withdraw strategy");
                    }
                }
                IERC1155(erc1155Token).safeTransferFrom(address(this), owner, tokenId, bal, "");
            } else {
                revert("unsupported token");
            }
        }
        emit WithdrawErc1155(owner, erc1155TokenNames, tokenIds, amounts, traceId);
    }

    function _withdraw1155Hook(
        bytes32 erc20TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) virtual internal returns (uint256 processedAmount){
        erc20TokenName;
        owner;
        tokenId;
        return amount;
    }
}
